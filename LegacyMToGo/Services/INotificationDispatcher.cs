using LegacyMToGo.Models;

namespace LegacyMToGo.Services;

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationMethod method, string destination, string message, CancellationToken cancellationToken = default);
}
