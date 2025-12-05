using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;

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

/// <summary>
/// Adapter that wraps the legacy notification API client.
/// Implements the Adapter pattern to provide a clean, modern interface while internally
/// using the legacy API. This allows the application to work with a consistent interface
/// regardless of the underlying legacy system's API design.
/// </summary>
public class LegacyNotificationAdapter : INotificationAdapter
{
    private readonly ILegacyNotificationApiClient _legacyClient;
    private readonly ILogger<LegacyNotificationAdapter> _logger;

    public LegacyNotificationAdapter(ILegacyNotificationApiClient legacyClient, ILogger<LegacyNotificationAdapter> logger)
    {
        _legacyClient = legacyClient;
        _logger = logger;
    }

    /// <summary>
    /// Adapts a modern notification request to the legacy API format and sends it.
    /// </summary>
    public async Task<NotificationAdapterResult> SendAsync(int customerId, string title, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adapting notification for customer {CustomerId}: {Title}", customerId, title);

        // Adapt the modern interface to the legacy API format
        var legacyRequest = AdaptToLegacyFormat(customerId, title, body);

        return await ExecuteLegacyRequestAsync(legacyRequest, customerId);
    }

    /// <summary>
    /// Sends an order status update notification using the legacy API.
    /// </summary>
    public async Task<NotificationAdapterResult> SendOrderUpdateAsync(int customerId, int orderId, string status, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending order update notification to customer {CustomerId} for order {OrderId}: {Status}", 
            customerId, orderId, status);

        var title = "Order Update";
        var body = $"Your order #{orderId} is now {status}.";

        var legacyRequest = AdaptToLegacyFormat(customerId, title, body);

        return await ExecuteLegacyRequestAsync(legacyRequest, customerId);
    }

    /// <summary>
    /// Sends a promotional notification using the legacy API.
    /// </summary>
    public async Task<NotificationAdapterResult> SendPromotionAsync(int customerId, string promotionTitle, string promotionDetails, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending promotion notification to customer {CustomerId}: {PromotionTitle}", 
            customerId, promotionTitle);

        var body = $"🎉 {promotionTitle}\n\n{promotionDetails}";

        var legacyRequest = AdaptToLegacyFormat(customerId, promotionTitle, body);

        return await ExecuteLegacyRequestAsync(legacyRequest, customerId);
    }

    /// <summary>
    /// Adapts the modern notification format to the legacy API request format.
    /// The legacy API expects a simple message string, so we combine title and body.
    /// </summary>
    private static NotificationRequest AdaptToLegacyFormat(int customerId, string title, string body)
    {
        // The legacy API only supports a single message field,
        // so we combine title and body into a formatted message
        var combinedMessage = string.IsNullOrWhiteSpace(title) 
            ? body 
            : $"[{title}] {body}";

        return new NotificationRequest
        {
            CustomerId = customerId,
            Message = combinedMessage
        };
    }

    /// <summary>
    /// Executes the legacy API request and adapts the response/exceptions to our modern result format.
    /// </summary>
    private async Task<NotificationAdapterResult> ExecuteLegacyRequestAsync(NotificationRequest request, int customerId)
    {
        try
        {
            var response = await _legacyClient.SendNotificationAsync(request);

            _logger.LogInformation("Notification sent successfully to customer {CustomerId}", customerId);

            return NotificationAdapterResult.Succeeded(response.Message);
        }
        catch (CustomerNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer {CustomerId} not found when sending notification", customerId);
            return NotificationAdapterResult.Failed(
                NotificationAdapterError.CustomerNotFound,
                $"Customer with ID {customerId} not found");
        }
        catch (NotificationFailedException ex)
        {
            _logger.LogError(ex, "Failed to send notification to customer {CustomerId}", customerId);
            return NotificationAdapterResult.Failed(
                NotificationAdapterError.ServiceUnavailable,
                "Failed to send notification through legacy service");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when sending notification to customer {CustomerId}", customerId);
            return NotificationAdapterResult.Failed(
                NotificationAdapterError.ServiceUnavailable,
                "Legacy notification service is unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when sending notification to customer {CustomerId}", customerId);
            return NotificationAdapterResult.Failed(
                NotificationAdapterError.Unknown,
                "An unexpected error occurred while sending the notification");
        }
    }
}
