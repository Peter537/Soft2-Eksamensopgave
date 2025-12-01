namespace MToGo.CustomerService.Models;

public record CustomerProfileResponse(
    string Name,
    string DeliveryAddress,
    string NotificationMethod,
    string? PhoneNumber,
    string? LanguagePreference
);
