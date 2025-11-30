using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Clients;

public interface ILegacyNotificationApiClient
{
    Task<NotificationResponse> SendNotificationAsync(NotificationRequest request);
}
