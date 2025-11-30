using System.Security.Claims;

namespace MToGo.Shared.Security;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string email, string role, string? name = null);
    
    ClaimsPrincipal? ValidateToken(string token);
    
    bool IsTokenExpired(string token);
    
    (int UserId, string Role)? GetUserInfoFromToken(string token);
}
