using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;
using MToGo.Shared.Kafka;
using Moq;
using MToGo.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.Kafka;

namespace MToGo.OrderService.Tests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly Mock<IKafkaProducer> _kafkaMock;
        private readonly Mock<IPartnerServiceClient> _partnerServiceClientMock;
        private readonly PostgreSqlContainer _postgresContainer;
        private readonly KafkaContainer _kafkaContainer;

        public TestWebApplicationFactory(Mock<IKafkaProducer> kafkaMock)
        {
            _kafkaMock = kafkaMock;
            _partnerServiceClientMock = new Mock<IPartnerServiceClient>();
            _partnerServiceClientMock
                .Setup(x => x.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Id = 1, Name = "Test Partner", Location = "Test Location" });
            _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer();
            _kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
            await _kafkaContainer.StartAsync();
            
            // Vær sikker på at databasen er oprettet
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                    ["Kafka:BootstrapServers"] = _kafkaContainer.GetBootstrapAddress()
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
            });
        }

        public new async ValueTask DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
            await _kafkaContainer.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}