extern alias CustomerServiceApp;
extern alias OrderServiceApp;

using System.Net.Http.Json;
using LegacyMToGo.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MToGo.CustomerService.Clients;
using MToGo.CustomerService.Models;
using MToGo.CustomerService.Tests.E2E;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.Testing;
using Moq;
using Testcontainers.PostgreSql;
using CustomerServiceProgram = CustomerServiceApp::Program;
using OrderServiceProgram = OrderServiceApp::Program;
using CustomerModel = MToGo.CustomerService.Models.Customer;

namespace MToGo.E2E.Tests.OrderService;

public class OrderJourneyFixture : IAsyncLifetime
{
    private PostgreSqlContainer _legacyDbContainer = null!;
    private OrderServicePostgresContainer _orderDbContainer = null!;
    private WebApplicationFactory<LegacyMToGo.Program> _legacyApiFactory = null!;
    private WebApplicationFactory<CustomerServiceProgram> _customerServiceFactory = null!;
    private OrderServiceWebApplicationFactory _orderServiceFactory = null!;
    private HttpClient _legacyClient = null!;

    public HttpClient CustomerClient { get; private set; } = null!;
    public HttpClient OrderClient { get; private set; } = null!;
    public IReadOnlyList<KafkaPublication> PublishedEvents => _orderServiceFactory.PublishedEvents;

    public async Task InitializeAsync()
    {
        _legacyDbContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer("legacy_mtogo");

        _orderDbContainer = new OrderServicePostgresContainer();

        await Task.WhenAll(_legacyDbContainer.StartAsync(), _orderDbContainer.InitializeAsync());

        _legacyApiFactory = new WebApplicationFactory<LegacyMToGo.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LegacyDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<LegacyDbContext>(options =>
                        options.UseNpgsql(_legacyDbContainer.GetConnectionString()));
                });
            });

        await EnsureLegacyDatabaseCreatedAsync();
        _legacyClient = _legacyApiFactory.CreateClient();

        _customerServiceFactory = new WebApplicationFactory<CustomerServiceProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILegacyCustomerApiClient));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    var httpClientDescriptors = services
                        .Where(d => d.ServiceType == typeof(IHttpClientFactory) ||
                                    d.ServiceType.FullName?.Contains("LegacyCustomerApiClient") == true)
                        .ToList();

                    foreach (var httpDescriptor in httpClientDescriptors)
                    {
                        services.Remove(httpDescriptor);
                    }

                    services.AddSingleton<ILegacyCustomerApiClient>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<LegacyCustomerApiClientForE2E>>();
                        return new LegacyCustomerApiClientForE2E(_legacyClient, logger);
                    });
                });
            });

        CustomerClient = _customerServiceFactory.CreateClient();

        _orderServiceFactory = new OrderServiceWebApplicationFactory(_orderDbContainer);
        await _orderServiceFactory.InitializeDatabaseAsync();
        OrderClient = _orderServiceFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        CustomerClient?.Dispose();
        OrderClient?.Dispose();
        _legacyClient?.Dispose();

        if (_orderServiceFactory != null)
        {
            await _orderServiceFactory.DisposeAsync();
        }

        if (_customerServiceFactory != null)
        {
            await _customerServiceFactory.DisposeAsync();
        }

        if (_legacyApiFactory != null)
        {
            await _legacyApiFactory.DisposeAsync();
        }

        if (_orderDbContainer != null)
        {
            await _orderDbContainer.DisposeAsync();
        }

        if (_legacyDbContainer != null)
        {
            await _legacyDbContainer.DisposeAsync();
        }
    }

    public async Task ResetStateAsync()
    {
        using (var scope = _legacyApiFactory.Services.CreateScope())
        {
            var legacyDb = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
            await legacyDb.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Customers\" RESTART IDENTITY CASCADE;");
        }

        await _orderServiceFactory.CleanupDatabaseAsync();
        _orderServiceFactory.ResetKafkaEvents();
        TestAuthenticationHandler.ClearTestUser();
    }

    public async Task<int> RegisterCustomerAsync(CustomerModel request)
    {
        var response = await CustomerClient.PostAsJsonAsync("/api/v1/customers", request);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreateCustomerResponse>();
        if (created == null)
        {
            throw new InvalidOperationException("Customer registration returned no payload.");
        }

        return created.Id;
    }

    public void RunAsCustomer(int customerId) => TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");
    public void RunAsPartner(int partnerId) => TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");
    public void RunAsAgent(int agentId) => TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

    private async Task EnsureLegacyDatabaseCreatedAsync()
    {
        using var scope = _legacyApiFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}

internal class OrderServicePostgresContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer();

    public string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }
}

internal class OrderServiceWebApplicationFactory : WebApplicationFactory<OrderServiceProgram>
{
    private readonly OrderServicePostgresContainer _container;
    private readonly Mock<IKafkaProducer> _kafkaMock = new();
    private readonly Mock<IPartnerServiceClient> _partnerServiceClientMock = new();
    private readonly Mock<IAgentServiceClient> _agentServiceClientMock = new();
    private readonly List<KafkaPublication> _publishedEvents = new();

    public IReadOnlyList<KafkaPublication> PublishedEvents => _publishedEvents;

    public OrderServiceWebApplicationFactory(OrderServicePostgresContainer container)
    {
        _container = container;

        _partnerServiceClientMock
            .Setup(x => x.GetPartnerByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new PartnerResponse
            {
                Id = id,
                Name = $"Partner #{id}",
                Address = "Østerbrogade 27, Copenhagen"
            });

        _agentServiceClientMock
            .Setup(x => x.GetAgentByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new AgentResponse
            {
                Id = id,
                Name = $"Agent #{id}"
            });

        CaptureKafkaEvent<OrderCreatedEvent>();
        CaptureKafkaEvent<OrderAcceptedEvent>();
        CaptureKafkaEvent<OrderReadyEvent>();
        CaptureKafkaEvent<AgentAssignedEvent>();
        CaptureKafkaEvent<OrderPickedUpEvent>();
        CaptureKafkaEvent<OrderDeliveredEvent>();
    }

    public void ResetKafkaEvents() => _publishedEvents.Clear();

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"OrderItems\" CASCADE;");
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Orders\" CASCADE;");
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Orders_Id_seq\" RESTART WITH 1;");
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"OrderItems_Id_seq\" RESTART WITH 1;");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.ConnectionString
            };

            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IKafkaProducer>(_kafkaMock.Object);

            var partnerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPartnerServiceClient));
            if (partnerDescriptor != null)
            {
                services.Remove(partnerDescriptor);
            }
            services.AddSingleton(_partnerServiceClientMock.Object);

            var agentDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAgentServiceClient));
            if (agentDescriptor != null)
            {
                services.Remove(agentDescriptor);
            }
            services.AddSingleton(_agentServiceClientMock.Object);

            services.AddTestAuthentication();
        });
    }

    private void CaptureKafkaEvent<TEvent>()
    {
        _kafkaMock
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TEvent>()))
            .Returns(Task.CompletedTask)
            .Callback<string, string, TEvent>((topic, key, payload) =>
            {
                _publishedEvents.Add(new KafkaPublication(topic, key, payload!));
            });
    }
}

public record KafkaPublication(string Topic, string Key, object Payload);

