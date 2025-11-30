using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly ILegacyNotificationApiClient _legacyApiClient;

    public NotificationService(ILegacyNotificationApiClient legacyApiClient)
    {
        _legacyApiClient = legacyApiClient;
    }

    public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
    {
        return await _legacyApiClient.SendNotificationAsync(request);
    }
}
