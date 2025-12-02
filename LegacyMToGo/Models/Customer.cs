namespace LegacyMToGo.Models;

public enum NotificationMethod
{
    Email,
    Sms,
    Push
}

public enum LanguagePreference
{
    En,  // English
    Da   // Danish
}

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string DeliveryAddress { get; set; }
    public required NotificationMethod NotificationMethod { get; set; }
    public required string Password { get; set; }
    public string? PhoneNumber { get; set; }
    public LanguagePreference LanguagePreference { get; set; } = LanguagePreference.En;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
