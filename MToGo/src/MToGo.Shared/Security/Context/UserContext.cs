namespace MToGo.Shared.Security.Context;

using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using System.Security.Claims;

public class UserContext : IUserContext
{
    public int? Id { get; }
    public string Role { get; }
    public string Email { get; }
    public bool IsAuthenticated { get; }
    public bool IsCustomer => Role == UserRoles.Customer;
    public bool IsAgent => Role == UserRoles.Agent;
    public bool IsPartner => Role == UserRoles.Partner;
    public bool IsManagement => Role == UserRoles.Management;

    public UserContext(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated == true)
        {
            IsAuthenticated = true;
            
            var idClaim = principal.FindFirst(JwtClaims.Id)?.Value;
            Id = int.TryParse(idClaim, out var id) ? id : null;
            
            Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            Email = principal.FindFirst(JwtClaims.Email)?.Value ?? string.Empty;
        }
        else
        {
            IsAuthenticated = false;
            Id = null;
            Role = string.Empty;
            Email = string.Empty;
        }
    }
}
