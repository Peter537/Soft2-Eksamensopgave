namespace MToGo.CustomerService.Models;

public record LegacyLoginResponse(
    int Id,
    string Name,
    string Email,
    string HashedPassword
);
