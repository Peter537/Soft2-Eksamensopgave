using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Services;

public interface INotificationService
{
    Task<NotificationResponse> SendNotificationAsync(NotificationRequest request);
}
