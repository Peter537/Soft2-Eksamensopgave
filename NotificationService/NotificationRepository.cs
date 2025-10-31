using NotificationService.Models;

namespace NotificationService
{
    public class NotificationRepository
    {
        private readonly List<Notification> _notifications = new();
        private readonly object _lock = new();

        public void AddNotification(Notification notification)
        {
            lock (_lock)
            {
                _notifications.Add(notification);
                
                // Beautiful console logging with emoji
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n{notification.Emoji} NEW NOTIFICATION CREATED");
                Console.WriteLine($"├─ Order: {notification.OrderId}");
                Console.WriteLine($"├─ Type: {notification.Type}");
                Console.WriteLine($"├─ Message: {notification.Message}");
                Console.WriteLine($"└─ Time: {notification.Timestamp:HH:mm:ss}");
                Console.ResetColor();
            }
        }

        public List<Notification> GetNotificationsByOrder(string orderId)
        {
            lock (_lock)
            {
                return _notifications
                    .Where(n => n.OrderId == orderId)
                    .OrderBy(n => n.Timestamp)
                    .ToList();
            }
        }

        public List<Notification> GetAllNotifications()
        {
            lock (_lock)
            {
                return _notifications.OrderByDescending(n => n.Timestamp).ToList();
            }
        }

        public List<Notification> GetUnreadNotifications()
        {
            lock (_lock)
            {
                return _notifications
                    .Where(n => !n.IsRead)
                    .OrderByDescending(n => n.Timestamp)
                    .ToList();
            }
        }

        public void MarkAsRead(string notificationId)
        {
            lock (_lock)
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                }
            }
        }
    }
}
