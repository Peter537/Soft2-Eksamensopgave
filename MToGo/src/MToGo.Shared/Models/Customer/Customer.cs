namespace MToGo.Shared.Models.Customer;

public class Customer
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string DeliveryAddress { get; set; }
    public required string NotificationMethod { get; set; }
    public required string Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string LanguagePreference { get; set; } = "en"; // "en" for English, "da" for Danish
}
