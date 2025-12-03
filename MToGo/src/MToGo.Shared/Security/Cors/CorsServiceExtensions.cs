using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MToGo.Shared.Security.Cors;

/// <summary>
/// Extension methods for configuring CORS in MToGo services.
/// </summary>
public static class CorsServiceExtensions
{
    /// <summary>
    /// Adds MToGo CORS configuration with trusted origins policy.
    /// This should be used by the Gateway service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration containing CorsSettings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMToGoCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettingsSection = configuration.GetSection(CorsSettings.SectionName);
        services.Configure<CorsSettings>(corsSettingsSection);

        var corsSettings = corsSettingsSection.Get<CorsSettings>() ?? new CorsSettings();

        // Register settings for middleware
        services.AddSingleton(corsSettings);

        services.AddCors(options =>
        {
            // Main policy for trusted frontend origins
            options.AddPolicy(CorsPolicies.TrustedOrigins, builder =>
            {
                ConfigureCorsPolicy(builder, corsSettings);
            });

            // WebSocket-specific policy (same origins, but ensures credentials are allowed)
            options.AddPolicy(CorsPolicies.WebSocketPolicy, builder =>
            {
                ConfigureCorsPolicy(builder, corsSettings, isWebSocket: true);
            });
        });

        return services;
    }

    /// <summary>
    /// Adds MToGo CORS configuration with default development settings.
    /// Allows localhost origins for development convenience.
    /// </summary>
    public static IServiceCollection AddMToGoCorsDevelopment(this IServiceCollection services)
    {
        var devSettings = new CorsSettings
        {
            AllowedOrigins = new[]
            {
                "http://localhost:8081",    // Website in Docker
                "http://localhost:5000",    // Website local dev
                "http://localhost:5001",    // Website local dev HTTPS
                "https://localhost:5001",
                "http://127.0.0.1:8081",
                "http://127.0.0.1:5000",
            },
            AllowCredentials = true,
            LogBlockedRequests = true,
            PreflightMaxAgeSeconds = 600
        };

        services.AddSingleton(devSettings);

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.TrustedOrigins, builder =>
            {
                ConfigureCorsPolicy(builder, devSettings);
            });

            options.AddPolicy(CorsPolicies.WebSocketPolicy, builder =>
            {
                ConfigureCorsPolicy(builder, devSettings, isWebSocket: true);
            });
        });

        return services;
    }

    private static void ConfigureCorsPolicy(CorsPolicyBuilder builder, CorsSettings settings, bool isWebSocket = false)
    {
        // Configure origins
        if (settings.AllowedOrigins.Length > 0)
        {
            builder.WithOrigins(settings.AllowedOrigins);
        }
        else
        {
            // Fallback: If no origins configured, log a warning but allow for dev purposes
            // In production, this should be explicitly configured
            builder.SetIsOriginAllowed(_ => true);
        }

        // Configure methods
        if (settings.AllowedMethods.Length > 0)
        {
            builder.WithMethods(settings.AllowedMethods);
        }
        else
        {
            builder.AllowAnyMethod();
        }

        // Configure headers
        if (settings.AllowedHeaders.Length > 0)
        {
            builder.WithHeaders(settings.AllowedHeaders);
        }
        else
        {
            builder.AllowAnyHeader();
        }

        // Configure exposed headers
        if (settings.ExposedHeaders.Length > 0)
        {
            builder.WithExposedHeaders(settings.ExposedHeaders);
        }
        else
        {
            // Always expose common headers for API responses
            builder.WithExposedHeaders("Token-Expired", "Content-Disposition");
        }

        // Configure credentials (required for auth cookies and WebSockets)
        if (settings.AllowCredentials)
        {
            builder.AllowCredentials();
        }

        // Set preflight cache duration
        builder.SetPreflightMaxAge(TimeSpan.FromSeconds(settings.PreflightMaxAgeSeconds));
    }
}
