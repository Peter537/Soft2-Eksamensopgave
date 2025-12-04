using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MToGo.Shared.Security.Authorization;

namespace MToGo.Shared.Security.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateToken(int userId, string email, string role, string? name = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtClaims.Id, userId.ToString()),
            new(JwtClaims.Email, email),
            new(JwtClaims.Role, role),
            new(ClaimTypes.Role, role) // ASP.NET Core uses ClaimTypes.Role for role-based authorization
        };

        if (!string.IsNullOrEmpty(name))
        {
            claims.Add(new Claim(JwtClaims.Name, name));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }
}
