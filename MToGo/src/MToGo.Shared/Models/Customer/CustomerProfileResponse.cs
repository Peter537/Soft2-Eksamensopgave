namespace MToGo.Shared.Models.Customer;

public record CustomerProfileResponse(
    string Name,
    string DeliveryAddress,
    string NotificationMethod,
    string? PhoneNumber,
    string? LanguagePreference
);
