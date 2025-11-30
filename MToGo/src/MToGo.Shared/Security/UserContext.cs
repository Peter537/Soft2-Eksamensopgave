using System.Security.Claims;

namespace MToGo.Shared.Security;

public interface IUserContext
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    string? Name { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

public class UserContext : IUserContext
{
    private readonly ClaimsPrincipal? _user;

    public UserContext(ClaimsPrincipal? user)
    {
        _user = user;
    }

    public int? UserId
    {
        get
        {
            var claim = _user?.FindFirst(JwtClaims.UserId);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public string? Email => _user?.FindFirst(JwtClaims.Email)?.Value;

    public string? Role => _user?.FindFirst(JwtClaims.Role)?.Value;

    public string? Name => _user?.FindFirst(JwtClaims.Name)?.Value;

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => _user?.IsInRole(role) ?? false;
}
