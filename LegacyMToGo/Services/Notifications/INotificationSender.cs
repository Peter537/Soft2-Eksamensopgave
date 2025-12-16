namespace LegacyMToGo.Services.Notifications;

public interface INotificationSender
{
    /// <summary>
    /// Sends a notification to the specified destination.
    /// </summary>
    /// <param name="destination">The destination address (email, phone number, or device token).</param>
    /// <param name="message">The message content to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync(string destination, string message, CancellationToken cancellationToken = default);
}
