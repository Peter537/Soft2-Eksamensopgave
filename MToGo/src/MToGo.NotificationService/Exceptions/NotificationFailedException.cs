namespace MToGo.NotificationService.Exceptions;

public class NotificationFailedException : Exception
{
    public NotificationFailedException(string message) : base(message)
    {
    }

    public NotificationFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
