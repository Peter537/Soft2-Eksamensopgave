using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MToGo.Shared.Security.Authentication;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Context;
using MToGo.Shared.Security.Password;
using System.Text;

namespace MToGo.Shared.Security;

public static class SecurityServiceExtensions
{
    /// <summary>
    /// Adds MToGo JWT authentication and authorization services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMToGoSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure JWT Settings
        var jwtSettingsSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSettingsSection);

        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() 
            ?? throw new InvalidOperationException("JWT Settings not configured. Please add JwtSettings section to appsettings.json");

        // Register security services
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserContextAccessor, UserContextAccessor>();

        // Configure Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero // No tolerance for expiration
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Add header to indicate token expiration
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Configure Authorization Policies for RBAC
        services.AddAuthorization(options =>
        {
            // Single role policies
            options.AddPolicy(AuthorizationPolicies.CustomerOnly, policy =>
                policy.RequireRole(UserRoles.Customer));
            
            options.AddPolicy(AuthorizationPolicies.PartnerOnly, policy =>
                policy.RequireRole(UserRoles.Partner));
            
            options.AddPolicy(AuthorizationPolicies.AgentOnly, policy =>
                policy.RequireRole(UserRoles.Agent));
            
            options.AddPolicy(AuthorizationPolicies.ManagementOnly, policy =>
                policy.RequireRole(UserRoles.Management));
            
            // Combined role policies
            options.AddPolicy(AuthorizationPolicies.CustomerOrManagement, policy =>
                policy.RequireRole(UserRoles.Customer, UserRoles.Management));
            
            options.AddPolicy(AuthorizationPolicies.PartnerOrManagement, policy =>
                policy.RequireRole(UserRoles.Partner, UserRoles.Management));
            
            options.AddPolicy(AuthorizationPolicies.AgentOrManagement, policy =>
                policy.RequireRole(UserRoles.Agent, UserRoles.Management));
            
            // Any authenticated user
            options.AddPolicy(AuthorizationPolicies.AllAuthenticated, policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    /// <summary>
    /// Adds MToGo security with WebSocket support (token from query string).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMToGoSecurityWithWebSockets(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMToGoSecurity(configuration);

        // Reconfigure JWT Bearer to support WebSocket connections (token in query string)
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var originalOnMessageReceived = options.Events?.OnMessageReceived;
            
            options.Events ??= new JwtBearerEvents();
            options.Events.OnMessageReceived = async context =>
            {
                if (originalOnMessageReceived != null)
                {
                    await originalOnMessageReceived(context);
                }

                // Check for token in query string (for WebSocket connections)
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Behind reverse proxies, IsWebSocketRequest can be unreliable depending on middleware order.
                // The Upgrade header is a reliable indicator of a WebSocket handshake.
                var upgradeHeader = context.Request.Headers.Upgrade.ToString();
                var isWebSocketHandshake =
                    context.HttpContext.WebSockets.IsWebSocketRequest ||
                    string.Equals(upgradeHeader, "websocket", StringComparison.OrdinalIgnoreCase);

                // Apply token from query string for WebSocket paths
                if (!string.IsNullOrEmpty(accessToken) && (isWebSocketHandshake || path.StartsWithSegments("/api/v1/ws")))
                {
                    context.Token = accessToken;
                }
            };
        });

        return services;
    }
}
