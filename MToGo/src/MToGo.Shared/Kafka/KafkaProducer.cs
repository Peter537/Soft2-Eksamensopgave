using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MToGo.Shared.Kafka
{
    public class KafkaProducerConfig
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public Acks? Acks { get; set; }
        public bool EnableIdempotence { get; set; } = true;
        public int? MessageTimeoutMs { get; set; }
        public int? RequestTimeoutMs { get; set; }
    }

    public class KafkaProducer : IDisposable, IAsyncDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public KafkaProducer(IOptions<KafkaProducerConfig> config, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = config.Value.BootstrapServers,
                Acks = config.Value.Acks ?? Acks.All,
                EnableIdempotence = config.Value.EnableIdempotence
            };

            if (config.Value.MessageTimeoutMs.HasValue)
                producerConfig.MessageTimeoutMs = config.Value.MessageTimeoutMs.Value;

            if (config.Value.RequestTimeoutMs.HasValue)
                producerConfig.RequestTimeoutMs = config.Value.RequestTimeoutMs.Value;

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task PublishAsync<T>(string topic, string key, T eventData)
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(eventData, _jsonOptions)
            };

            try
            {
                var result = await _producer.ProduceAsync(topic, message);
                _logger.LogInformation("Published to {Topic} with key {Key}, partition {Partition}, offset {Offset}", topic, key, result.Partition, result.Offset);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Failed to publish to {Topic}: {Reason}", topic, ex.Error.Reason);
                throw;
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_producer != null)
            {
                try
                {
                    _producer.Flush(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during producer flush");
                }
                _producer.Dispose();
            }
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
