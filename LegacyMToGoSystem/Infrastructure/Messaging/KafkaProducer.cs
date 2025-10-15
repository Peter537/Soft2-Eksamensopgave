using Confluent.Kafka;
using System.Text.Json;

namespace LegacyMToGoSystem.Infrastructure.Messaging;

public class KafkaProducer : IDisposable
{
    private readonly IProducer<string, string>? _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly bool _isAvailable;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        
        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                ClientId = "mtogo-legacy-system",
                Acks = Acks.Leader,
                MessageTimeoutMs = 3000,
                LogConnectionClose = false,
                Debug = ""
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetLogHandler((_, logMessage) =>
                {
                    if (logMessage.Level <= SyslogLevel.Warning)
                    {
                        return;
                    }
                })
                .Build();
            _isAvailable = true;
            _logger.LogInformation("✅ Kafka producer initialized successfully");
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            _logger.LogWarning("⚠️ Kafka is not available. Running in fallback mode. Error: {Message}", ex.Message);
        }
    }

    public async Task PublishEventAsync<T>(string topic, string key, T eventData)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("Kafka unavailable. Event would have been published to topic '{Topic}': {Event}", 
                topic, JsonSerializer.Serialize(eventData));
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(eventData);
            var message = new Message<string, string>
            {
                Key = key,
                Value = json
            };

            var result = await _producer!.ProduceAsync(topic, message);
            _logger.LogDebug("Event published to Kafka topic '{Topic}' at offset {Offset}", topic, result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to Kafka topic '{Topic}'", topic);
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
