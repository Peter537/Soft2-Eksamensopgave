extern alias OrderServiceApp;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;
using MToGo.Shared.Kafka;
using MToGo.Testing;
using Moq;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using OrderServiceProgram = OrderServiceApp::Program;

namespace MToGo.Integration.Tests.OrderService.Fixtures;

public class OrderServiceIntegrationFixture : IAsyncLifetime
{
    private readonly OrderServiceContainerFixture _containers = new();

    public OrderServiceWebApplicationFactory Factory { get; private set; } = null!;

    public HttpClient CreateClient() => Factory.CreateClient();

    public Mock<IKafkaProducer> KafkaProducerMock => Factory.KafkaMock;

    public async Task InitializeAsync()
    {
        await _containers.InitializeAsync();
        Factory = new OrderServiceWebApplicationFactory(_containers);
        await Factory.InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory != null)
        {
            await Factory.DisposeAsync();
        }

        await _containers.DisposeAsync();
    }

    public async Task ResetStateAsync()
    {
        await Factory.CleanupDatabaseAsync();
        TestAuthenticationHandler.ClearTestUser();
    }
}

public class OrderServiceWebApplicationFactory : WebApplicationFactory<OrderServiceProgram>
{
    private readonly OrderServiceContainerFixture _fixture;
    private readonly Mock<IKafkaProducer> _kafkaMock = new();
    private readonly Mock<IPartnerServiceClient> _partnerServiceClientMock = new();
    private readonly Mock<IAgentServiceClient> _agentServiceClientMock = new();

    public Mock<IKafkaProducer> KafkaMock => _kafkaMock;

    public OrderServiceWebApplicationFactory(OrderServiceContainerFixture fixture)
    {
        _fixture = fixture;

        _partnerServiceClientMock
            .Setup(x => x.GetPartnerByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new PartnerResponse { Id = id, Name = $"Partner {id}", Address = "Test Address" });

        _agentServiceClientMock
            .Setup(x => x.GetAgentByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new AgentResponse { Id = id, Name = $"Agent {id}" });
    }

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
                ["ConnectionStrings:DefaultConnection"] = _fixture.PostgresConnectionString,
                ["Kafka:BootstrapServers"] = _fixture.KafkaBootstrapServers
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

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.AuthenticationScheme,
                _ => { });
        });
    }
}

public class OrderServiceContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly KafkaContainer _kafkaContainer;

    public OrderServiceContainerFixture()
    {
        _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer();
        _kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
    }

    public string PostgresConnectionString => _postgresContainer.GetConnectionString();
    public string KafkaBootstrapServers => _kafkaContainer.GetBootstrapAddress();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _kafkaContainer.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.DisposeAsync().AsTask(),
            _kafkaContainer.DisposeAsync().AsTask());
    }
}

[CollectionDefinition("OrderService Integration Tests")]
public class OrderServiceIntegrationCollection : ICollectionFixture<OrderServiceIntegrationFixture>
{
}
