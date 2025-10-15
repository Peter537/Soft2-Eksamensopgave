using LegacyMToGoSystem.Models;

namespace LegacyMToGoSystem.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByCustomerIdAsync(int customerId);
    Task<Notification> CreateAsync(Notification notification);
}
