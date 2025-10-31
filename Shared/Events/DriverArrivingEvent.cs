namespace Shared.Events;

public class DriverArrivingEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int EstimatedMinutes { get; set; } = 2;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
