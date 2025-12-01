namespace LegacyMToGo.Models;

public record CustomerCreateRequest(
    string Name,
    string Email,
    string DeliveryAddress,
    string NotificationMethod,
    string Password,
    string PhoneNumber,
    string? LanguagePreference = "en"
);
