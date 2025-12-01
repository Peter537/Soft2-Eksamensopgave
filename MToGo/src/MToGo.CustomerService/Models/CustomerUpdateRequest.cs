namespace MToGo.CustomerService.Models;

public record CustomerUpdateRequest(
    string? Name,
    string? DeliveryAddress,
    string? NotificationMethod,
    string? PhoneNumber,
    string? LanguagePreference
);
