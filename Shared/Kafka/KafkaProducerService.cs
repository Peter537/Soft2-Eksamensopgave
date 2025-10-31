using Confluent.Kafka;
using System.Text.Json;

namespace Shared.Kafka;

public class KafkaProducerService
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducerService(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All, // Wait for all replicas
            EnableIdempotence = true // Prevent duplicates
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T eventData)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(eventData)
            };

            var result = await _producer.ProduceAsync(topic, message);
            
            Console.WriteLine($"      ✅ Kafka: Published to '{topic}' | Key: {key} | Partition: {result.Partition} | Offset: {result.Offset}");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($"      ❌ Kafka Error: Failed to publish to '{topic}' - {ex.Error.Reason}");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
