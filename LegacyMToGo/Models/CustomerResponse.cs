namespace LegacyMToGo.Models;

public record CustomerResponse(
    string Name,
    string DeliveryAddress,
    string NotificationMethod,
    string? PhoneNumber,
    string? LanguagePreference
);
