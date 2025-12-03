namespace LegacyMToGo.Services.Notifications;

/// <summary>
/// Notification sender for push notification channel.
/// </summary>
public class PushNotificationSender : INotificationSender
{
    private readonly ILogger<PushNotificationSender> _logger;

    public PushNotificationSender(ILogger<PushNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string destination, string message, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = SanitizeMessage(message);

        // In a real implementation, this would integrate with a push service (Firebase, APNs, etc.)
        _logger.LogInformation("Sending PUSH notification to device {DeviceToken}: {Message}", destination, sanitizedMessage);

        return Task.CompletedTask;
    }

    private static string SanitizeMessage(string? message)
    {
        return message?
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ") ?? string.Empty;
    }
}
