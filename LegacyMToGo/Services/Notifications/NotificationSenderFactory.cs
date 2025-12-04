using LegacyMToGo.Models;

namespace LegacyMToGo.Services.Notifications;

/// <summary>
/// Factory for creating notification senders based on the notification method.
/// Uses the Factory Method pattern to encapsulate the creation logic.
/// </summary>
public class NotificationSenderFactory : INotificationSenderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationSenderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates the appropriate notification sender based on the notification method.
    /// </summary>
    /// <param name="method">The notification method.</param>
    /// <returns>The notification sender for the specified method.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported notification method is provided.</exception>
    public INotificationSender CreateSender(NotificationMethod method)
    {
        return method switch
        {
            NotificationMethod.Email => _serviceProvider.GetRequiredService<EmailNotificationSender>(),
            NotificationMethod.Sms => _serviceProvider.GetRequiredService<SmsNotificationSender>(),
            NotificationMethod.Push => _serviceProvider.GetRequiredService<PushNotificationSender>(),
            _ => throw new ArgumentException($"Unsupported notification method: {method}", nameof(method))
        };
    }
}
