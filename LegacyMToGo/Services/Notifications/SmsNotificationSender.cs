namespace LegacyMToGo.Services.Notifications;

/// <summary>
/// Notification sender for SMS channel.
/// </summary>
public class SmsNotificationSender : INotificationSender
{
    private readonly ILogger<SmsNotificationSender> _logger;

    public SmsNotificationSender(ILogger<SmsNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string destination, string message, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = SanitizeMessage(message);

        // In a real implementation, this would integrate with an SMS gateway (Twilio, etc.)
        _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}", destination, sanitizedMessage);

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
