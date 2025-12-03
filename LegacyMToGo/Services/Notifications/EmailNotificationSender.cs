namespace LegacyMToGo.Services.Notifications;

/// <summary>
/// Notification sender for email channel.
/// </summary>
public class EmailNotificationSender : INotificationSender
{
    private readonly ILogger<EmailNotificationSender> _logger;

    public EmailNotificationSender(ILogger<EmailNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string destination, string message, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = SanitizeMessage(message);

        // In a real implementation, this would integrate with an email service (SendGrid, SMTP, etc.)
        _logger.LogInformation("Sending EMAIL to {EmailAddress}: {Message}", destination, sanitizedMessage);

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
