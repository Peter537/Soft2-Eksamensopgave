namespace MToGo.NotificationService.Adapters;

/// <summary>
/// Modern notification interface that defines how the application wants to send notifications.
/// This is the "target" interface in the Adapter pattern - the interface that our application expects.
/// </summary>
public interface INotificationAdapter
{
    /// <summary>
    /// Sends a notification to a customer with a specific title and body.
    /// </summary>
    /// <param name="customerId">The customer ID to notify.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body/content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the notification operation.</returns>
    Task<NotificationAdapterResult> SendAsync(int customerId, string title, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an order update notification to a customer.
    /// </summary>
    /// <param name="customerId">The customer ID to notify.</param>
    /// <param name="orderId">The order ID.</param>
    /// <param name="status">The new order status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the notification operation.</returns>
    Task<NotificationAdapterResult> SendOrderUpdateAsync(int customerId, int orderId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a promotional notification to a customer.
    /// </summary>
    /// <param name="customerId">The customer ID to notify.</param>
    /// <param name="promotionTitle">The promotion title.</param>
    /// <param name="promotionDetails">The promotion details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the notification operation.</returns>
    Task<NotificationAdapterResult> SendPromotionAsync(int customerId, string promotionTitle, string promotionDetails, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a notification adapter operation.
/// </summary>
public class NotificationAdapterResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public NotificationAdapterError? Error { get; init; }

    public static NotificationAdapterResult Succeeded(string? message = null) => new()
    {
        Success = true,
        Message = message ?? "Notification sent successfully"
    };

    public static NotificationAdapterResult Failed(NotificationAdapterError error, string message) => new()
    {
        Success = false,
        Error = error,
        Message = message
    };
}

/// <summary>
/// Possible errors from the notification adapter.
/// </summary>
public enum NotificationAdapterError
{
    CustomerNotFound,
    ServiceUnavailable,
    InvalidRequest,
    Unknown
}
