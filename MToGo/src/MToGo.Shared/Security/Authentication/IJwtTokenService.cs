namespace MToGo.Shared.Security.Authentication;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string email, string role, string? name = null);
}
