namespace MToGo.NotificationService.Models;

public class NotificationRequest
{
    public int CustomerId { get; set; }
    public string Message { get; set; } = string.Empty;
}
