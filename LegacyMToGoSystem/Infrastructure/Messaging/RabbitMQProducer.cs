using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace LegacyMToGoSystem.Infrastructure.Messaging;

public class RabbitMQProducer : IDisposable
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly ILogger<RabbitMQProducer> _logger;
    private readonly bool _isAvailable;

    public RabbitMQProducer(IConfiguration configuration, ILogger<RabbitMQProducer> logger)
    {
        _logger = logger;
        
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            
            _channel.QueueDeclareAsync(queue: "orders", durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
            _channel.QueueDeclareAsync(queue: "payments", durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
            
            _isAvailable = true;
            _logger.LogInformation("✅ RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            _logger.LogWarning("⚠️ RabbitMQ is not available. Running in fallback mode. Error: {Message}", ex.Message);
        }
    }

    public void PublishMessage<T>(string queue, T message)
    {
        if (!_isAvailable)
        {
            _logger.LogDebug("RabbitMQ unavailable. Message would have been sent to queue '{Queue}': {Message}", 
                queue, JsonSerializer.Serialize(message));
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel!.BasicPublishAsync(exchange: "", routingKey: queue, body: body).GetAwaiter().GetResult();
            _logger.LogDebug("Message published to RabbitMQ queue '{Queue}'", queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ queue '{Queue}'", queue);
        }
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
}
