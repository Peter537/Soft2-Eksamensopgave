namespace MToGo.Shared.Models.Customer;

public record CustomerUpdateRequest(
    string? Name,
    string? DeliveryAddress,
    string? NotificationMethod,
    string? PhoneNumber,
    string? LanguagePreference
);
