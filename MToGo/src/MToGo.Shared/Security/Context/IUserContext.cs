namespace MToGo.Shared.Security.Context;

public interface IUserContext
{
    int? Id { get; }
    string Role { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool IsCustomer { get; }
    bool IsAgent { get; }
    bool IsPartner { get; }
    bool IsManagement { get; }
}
