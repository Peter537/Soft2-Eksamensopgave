using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MToGo.Testing;

/// <summary>
/// A test authentication handler that allows tests to bypass JWT authentication
/// by providing a configured identity with claims.
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    
    public static string? TestUserId { get; set; }
    public static string? TestUserEmail { get; set; }
    public static string? TestUserRole { get; set; }
    public static string? TestUserName { get; set; }

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if test credentials are set
        if (string.IsNullOrEmpty(TestUserId) || string.IsNullOrEmpty(TestUserRole))
        {
            return Task.FromResult(AuthenticateResult.Fail("No test credentials configured"));
        }

        var claims = new List<Claim>
        {
            new("id", TestUserId),
            new("email", TestUserEmail ?? "test@example.com"),
            new("role", TestUserRole),
            new(ClaimTypes.Role, TestUserRole),
            new("name", TestUserName ?? "Test User")
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Configure test credentials for the next request
    /// </summary>
    public static void SetTestUser(string userId, string role, string? email = null, string? name = null)
    {
        TestUserId = userId;
        TestUserRole = role;
        TestUserEmail = email ?? $"{userId}@test.com";
        TestUserName = name ?? "Test User";
    }

    /// <summary>
    /// Clear test credentials
    /// </summary>
    public static void ClearTestUser()
    {
        TestUserId = null;
        TestUserEmail = null;
        TestUserRole = null;
        TestUserName = null;
    }
}

