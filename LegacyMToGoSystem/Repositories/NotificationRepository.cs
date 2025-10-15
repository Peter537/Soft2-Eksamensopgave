using LegacyMToGoSystem.Infrastructure;
using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IDatabase _database;
    private const string CollectionName = "notifications";

    public NotificationRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task<IEnumerable<Notification>> GetByCustomerIdAsync(int customerId)
    {
        var notifications = await LoadNotificationsAsync();
        return notifications.Where(n => n.CustomerId == customerId).OrderByDescending(n => n.CreatedAt).ToList();
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        var notifications = await LoadNotificationsAsync();
        
        notification.Id = notifications.Any() ? notifications.Max(n => n.Id) + 1 : 1;
        notification.CreatedAt = DateTime.UtcNow;
        
        notifications.Add(notification);
        await SaveNotificationsAsync(notifications);
        
        return notification;
    }

    private async Task<List<Notification>> LoadNotificationsAsync()
    {
        var notifications = await _database.LoadAsync<List<Notification>>(CollectionName);
        return notifications ?? new List<Notification>();
    }

    private async Task SaveNotificationsAsync(List<Notification> notifications)
    {
        await _database.SaveAsync(CollectionName, notifications);
    }
}
