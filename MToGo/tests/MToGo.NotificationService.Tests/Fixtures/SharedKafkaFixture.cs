using Testcontainers.Kafka;
using MToGo.Testing;

namespace MToGo.NotificationService.Tests.Fixtures;

/// <summary>
/// Shared Kafka container fixture for NotificationService tests.
/// Container is started once and shared across all tests in the assembly.
/// </summary>
public class SharedKafkaFixture : IAsyncLifetime
{
    private readonly KafkaContainer _kafkaContainer;

    public string BootstrapServers => _kafkaContainer.GetBootstrapAddress();

    public SharedKafkaFixture()
    {
        _kafkaContainer = KafkaContainerHelper.CreateKafkaContainer();
    }

    public async Task InitializeAsync()
    {
        await _kafkaContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _kafkaContainer.DisposeAsync();
    }
}

/// <summary>
/// Collection definition that ensures all tests share the same Kafka container.
/// All test classes with [Collection("Kafka")] will share this single fixture instance.
/// </summary>
[CollectionDefinition("Kafka")]
public class KafkaCollection : ICollectionFixture<SharedKafkaFixture>
{
}
