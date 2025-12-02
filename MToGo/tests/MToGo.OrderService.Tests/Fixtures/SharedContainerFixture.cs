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

namespace MToGo.OrderService.Tests.Fixtures
{
    public class SharedContainerFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer;
        private readonly KafkaContainer _kafkaContainer;
        private bool _databaseInitialized;

        public string PostgresConnectionString => _postgresContainer.GetConnectionString();
        public string KafkaBootstrapServers => _kafkaContainer.GetBootstrapAddress();

        public SharedContainerFixture()
        {
            _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer();
            _kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        }

        public async Task InitializeAsync()
        {
            // Start both containers in parallel for faster startup
            await Task.WhenAll(
                _postgresContainer.StartAsync(),
                _kafkaContainer.StartAsync()
            );
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(
                _postgresContainer.DisposeAsync().AsTask(),
                _kafkaContainer.DisposeAsync().AsTask()
            );
        }

        public bool IsDatabaseInitialized => _databaseInitialized;
        public void MarkDatabaseInitialized() => _databaseInitialized = true;
    }

    public class SharedTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SharedContainerFixture _fixture;
        private readonly Mock<IKafkaProducer> _kafkaMock;
        private readonly Mock<IPartnerServiceClient> _partnerServiceClientMock;
        private readonly Mock<IAgentServiceClient> _agentServiceClientMock;

        public Mock<IKafkaProducer> KafkaMock => _kafkaMock;
        public Mock<IPartnerServiceClient> PartnerServiceClientMock => _partnerServiceClientMock;
        public Mock<IAgentServiceClient> AgentServiceClientMock => _agentServiceClientMock;

        public SharedTestWebApplicationFactory(SharedContainerFixture fixture)
        {
            _fixture = fixture;
            _kafkaMock = new Mock<IKafkaProducer>();
            _partnerServiceClientMock = new Mock<IPartnerServiceClient>();
            _partnerServiceClientMock
                .Setup(x => x.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Id = 1, Name = "Test Partner", Address = "Test Address" });
            _agentServiceClientMock = new Mock<IAgentServiceClient>();
            _agentServiceClientMock
                .Setup(x => x.GetAgentByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => new AgentResponse { Id = id, Name = $"Test Agent {id}" });
        }

        public async Task InitializeDatabaseAsync()
        {
            if (!_fixture.IsDatabaseInitialized)
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
                _fixture.MarkDatabaseInitialized();
            }
        }

        public async Task CleanupDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            
            // Clear all data but keep the schema using raw SQL for efficiency
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"OrderItems\" CASCADE;");
            await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Orders\" CASCADE;");
            
            // Reset identity sequences for predictable IDs in tests
            await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Orders_Id_seq\" RESTART WITH 1;");
            await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"OrderItems_Id_seq\" RESTART WITH 1;");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _fixture.PostgresConnectionString,
                    ["Kafka:BootstrapServers"] = _fixture.KafkaBootstrapServers
                };
                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IKafkaProducer>(_kafkaMock.Object);

                // Remove existing IPartnerServiceClient registration and add mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPartnerServiceClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IPartnerServiceClient>(_partnerServiceClientMock.Object);

                // Remove existing IAgentServiceClient registration and add mock
                var agentDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAgentServiceClient));
                if (agentDescriptor != null)
                {
                    services.Remove(agentDescriptor);
                }
                services.AddSingleton<IAgentServiceClient>(_agentServiceClientMock.Object);

                // Add test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme, options => { });
            });
        }
    }
}
