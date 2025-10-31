namespace Shared.Events;

public class LocationUpdateEvent
{
    public string OrderId { get; set; } = "";
    public string DriverId { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}
