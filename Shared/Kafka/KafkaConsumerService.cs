using Confluent.Kafka;
using System.Text.Json;

namespace Shared.Kafka;

public class KafkaConsumerService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly CancellationTokenSource _cts = new();

    public KafkaConsumerService(string bootstrapServers, string groupId, string topic)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest, // Start from beginning if no offset
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false // Manual offset control
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(topic);
        
        Console.WriteLine($"   📡 Kafka Consumer subscribed to topic: '{topic}' (GroupId: {groupId})");
    }

    public async Task ConsumeAsync<T>(Func<T, Task> handler)
    {
        await Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(_cts.Token);
                        
                        if (consumeResult?.Message?.Value != null)
                        {
                            var eventData = JsonSerializer.Deserialize<T>(consumeResult.Message.Value);
                            
                            if (eventData != null)
                            {
                                Console.WriteLine($"\n   📥 Kafka: Received from '{consumeResult.Topic}' | Key: {consumeResult.Message.Key}");
                                await handler(eventData);
                                
                                // Commit offset after successful processing
                                _consumer.StoreOffset(consumeResult);
                            }
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"❌ Consume error: {ex.Error.Reason}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Handler error: {ex.Message}");
                    }
                }
            }
            finally
            {
                _consumer.Close();
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _consumer?.Dispose();
        _cts?.Dispose();
    }
}
