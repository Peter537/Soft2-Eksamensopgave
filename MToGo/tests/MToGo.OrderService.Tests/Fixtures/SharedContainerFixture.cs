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

namespace MToGo.OrderService.Tests.Fixtures
{
    public class SharedTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"OrderServiceTests_{Guid.NewGuid():N}";
        private readonly Mock<IKafkaProducer> _kafkaMock;
        private readonly Mock<IPartnerServiceClient> _partnerServiceClientMock;
        private readonly Mock<IAgentServiceClient> _agentServiceClientMock;

        public Mock<IKafkaProducer> KafkaMock => _kafkaMock;
        public Mock<IPartnerServiceClient> PartnerServiceClientMock => _partnerServiceClientMock;
        public Mock<IAgentServiceClient> AgentServiceClientMock => _agentServiceClientMock;

        public SharedTestWebApplicationFactory()
        {
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
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        public async Task CleanupDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // For in-memory provider, EnsureDeleted/EnsureCreated is the simplest clean reset.
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    // The real DB/Kafka are not used in tests; we override DB and mock IKafkaProducer.
                    ["ConnectionStrings:DefaultConnection"] = "InMemory",
                    ["Kafka:BootstrapServers"] = "Mock"
                };
                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                // Replace the real DB provider with an in-memory one for tests.
                var dbContextOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                if (dbContextOptionsDescriptor != null)
                {
                    services.Remove(dbContextOptionsDescriptor);
                }
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(OrderDbContext));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }
                services.AddDbContext<OrderDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

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
                services.AddTestAuthentication();
            });
        }
    }
}

