namespace MToGo.Shared.Security.Authentication;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    public string SecretKey { get; set; } = string.Empty;
    
    public string Issuer { get; set; } = string.Empty;
    
    public string Audience { get; set; } = string.Empty;
    
    public int ExpirationMinutes { get; set; } = 60;
    
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
