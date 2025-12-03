using LegacyMToGo.Models;

namespace LegacyMToGo.Services.Notifications;

public interface INotificationSenderFactory
{
    INotificationSender CreateSender(NotificationMethod method);
}
