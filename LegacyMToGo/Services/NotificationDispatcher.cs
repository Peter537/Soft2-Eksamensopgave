using LegacyMToGo.Models;

namespace LegacyMToGo.Services;

public class NotificationDispatcher(ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public Task DispatchAsync(NotificationMethod method, string destination, string message, CancellationToken cancellationToken = default)
    {
        var sanitizedMessage = message?
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
            
        logger.LogInformation("Legacy notification via {Method} to {Destination}: {Message}", method, destination, sanitizedMessage);
        return Task.CompletedTask;
    }
}
