using LegacyMToGoSystem.Models;
using LegacyMToGoSystem.Repositories;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace LegacyMToGoSystem.Services;

public class NotificationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IServiceProvider services, ILogger<NotificationService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public async Task ProcessOrderEventAsync(string eventType, object eventData)
    {
        using var scope = _services.CreateScope();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var json = JsonSerializer.Serialize(eventData);
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        if (data == null || !data.ContainsKey("orderId")) return;

        var orderId = data["orderId"].GetInt32();
        var order = await orderRepo.GetByIdAsync(orderId);
        if (order == null) return;

        var message = eventType switch
        {
            "OrderPlaced" => "Your order has been placed successfully!",
            "OrderPreparing" => "Your order is now being prepared.",
            "AgentAssigned" => "A delivery agent has been assigned to your order.",
            "OrderInTransit" => "Your order is on the way!",
            "OrderDelivered" => "Your order has been delivered. Enjoy your meal!",
            _ => null
        };

        if (message != null)
        {
            var notification = new Notification
            {
                CustomerId = order.CustomerId,
                OrderId = orderId,
                Message = message,
                Type = eventType
            };

            await notificationRepo.CreateAsync(notification);
            _logger.LogInformation($"Notification created: {message} (Order {orderId})");
        }
    }
}
