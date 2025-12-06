using MToGo.NotificationService.Clients;
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
    private readonly ILegacyNotificationApiClient _legacyApiClient;

    public NotificationService(ILegacyNotificationApiClient legacyApiClient)
    {
        _legacyApiClient = legacyApiClient;
    }

    /// <summary>
    /// Delegates notification delivery to the legacy API client.
    /// </summary>
    public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
    {
        return await _legacyApiClient.SendNotificationAsync(request);
    }
}
