using LegacyMToGo.Models;
using LegacyMToGo.Services.Notifications;

namespace LegacyMToGo.Services;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationMethod method, string destination, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dispatches notifications using the Factory pattern.
/// Delegates the actual sending to the appropriate notification sender created by the factory.
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationSenderFactory _senderFactory;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(INotificationSenderFactory senderFactory, ILogger<NotificationDispatcher> logger)
    {
        _senderFactory = senderFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationMethod method, string destination, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching notification via {Method} to {Destination}", method, destination);

        var sender = _senderFactory.CreateSender(method);
        await sender.SendAsync(destination, message, cancellationToken);

        _logger.LogInformation("Notification dispatched successfully via {Method}", method);
    }
}
