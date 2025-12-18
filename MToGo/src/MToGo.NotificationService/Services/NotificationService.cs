using MToGo.NotificationService.Adapters;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Services;

public interface INotificationService
{
    /// <summary>
    /// Sends a notification through the legacy notification API.
    /// </summary>
    Task<NotificationResponse> SendNotificationAsync(NotificationRequest request);
}

public class NotificationService : INotificationService
{
    private readonly INotificationAdapter _notificationAdapter;

    public NotificationService(INotificationAdapter notificationAdapter)
    {
        _notificationAdapter = notificationAdapter;
    }

    /// <summary>
    /// Delegates notification delivery to the legacy API via the adapter.
    /// </summary>
    public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
    {
        var result = await _notificationAdapter.SendAsync(request.CustomerId, string.Empty, request.Message);

        if (!result.Success)
        {
            switch (result.Error)
            {
                case NotificationAdapterError.CustomerNotFound:
                    throw new CustomerNotFoundException(result.Message ?? $"Customer with ID {request.CustomerId} not found.");
                case NotificationAdapterError.ServiceUnavailable:
                    throw new NotificationFailedException(result.Message ?? "Legacy notification service unavailable.");
                case NotificationAdapterError.InvalidRequest:
                    throw new NotificationFailedException(result.Message ?? "Invalid notification request.");
                default:
                    throw new NotificationFailedException(result.Message ?? "An unexpected error occurred while sending the notification.");
            }
        }

        return new NotificationResponse
        {
            Success = true,
            Message = result.Message ?? "Notification sent successfully"
        };
    }
}
