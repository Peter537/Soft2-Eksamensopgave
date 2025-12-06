namespace MToGo.NotificationService.Models
{
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
}
