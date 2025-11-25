using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MToGo.Shared.Kafka
{
    public class KafkaConsumerConfig
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public AutoOffsetReset? AutoOffsetReset { get; set; }
        public bool EnableAutoCommit { get; set; } = true;
        public bool EnableAutoOffsetStore { get; set; } = false;
        public int? SessionTimeoutMs { get; set; }
        public int? HeartbeatIntervalMs { get; set; }
    }

    public class KafkaConsumer : IDisposable, IAsyncDisposable
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly CancellationTokenSource _cts = new();

        public KafkaConsumer(IOptions<KafkaConsumerConfig> config, ILogger<KafkaConsumer> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = config.Value.BootstrapServers,
                GroupId = config.Value.GroupId,
                AutoOffsetReset = config.Value.AutoOffsetReset ?? AutoOffsetReset.Earliest,
                EnableAutoCommit = config.Value.EnableAutoCommit,
                EnableAutoOffsetStore = config.Value.EnableAutoOffsetStore
            };

            if (config.Value.SessionTimeoutMs.HasValue)
                consumerConfig.SessionTimeoutMs = config.Value.SessionTimeoutMs.Value;

            if (config.Value.HeartbeatIntervalMs.HasValue)
                consumerConfig.HeartbeatIntervalMs = config.Value.HeartbeatIntervalMs.Value;

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe(config.Value.Topics);

            _logger.LogInformation("Kafka Consumer subscribed to topics: {Topics} (GroupId: {GroupId})", string.Join(", ", config.Value.Topics), config.Value.GroupId);
        }

        public async Task ConsumeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);

            await Task.Run(async () =>
            {
                try
                {
                    while (!linkedCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = _consumer.Consume(linkedCts.Token);

                            if (consumeResult?.Message?.Value != null)
                            {
                                var eventData = JsonSerializer.Deserialize<T>(consumeResult.Message.Value, _jsonOptions);

                                if (eventData != null)
                                {
                                    _logger.LogInformation("Received from '{Topic}' | Key: {Key}, Partition: {Partition}, Offset: {Offset}",
                                        consumeResult.Topic, consumeResult.Message.Key, consumeResult.Partition, consumeResult.Offset);
                                    await handler(eventData);

                                    // Commit offset after successful processing
                                    _consumer.StoreOffset(consumeResult);
                                }
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Deserialization error: {Message}", ex.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Handler error: {Message}", ex.Message);
                        }
                    }
                }
                finally
                {
                    _consumer.Close();
                }
            }, linkedCts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _consumer?.Close();
            _consumer?.Dispose();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
