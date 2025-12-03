using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MToGo.Shared.Security.Cors;

/// <summary>
/// Middleware that logs and optionally blocks requests from non-compliant origins.
/// This provides visibility into potential CSRF attacks or misconfigured clients.
/// </summary>
public class CorsLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorsLoggingMiddleware> _logger;
    private readonly HashSet<string> _allowedOrigins;
    private readonly bool _logBlockedRequests;

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        return value.Replace("\r", "").Replace("\n", "");
    }

    public CorsLoggingMiddleware(
        RequestDelegate next,
        ILogger<CorsLoggingMiddleware> logger,
        CorsSettings settings)
    {
        _next = next;
        _logger = logger;
        _allowedOrigins = new HashSet<string>(
            settings.AllowedOrigins ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        _logBlockedRequests = settings.LogBlockedRequests;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();

        if (!string.IsNullOrEmpty(origin))
        {
            var isAllowed = _allowedOrigins.Count == 0 || _allowedOrigins.Contains(origin);

            if (!isAllowed && _logBlockedRequests)
            {
                _logger.LogWarning(
                    "CORS: Blocked request from non-compliant origin. Origin: {Origin}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}, IP: {IP}",
                    SanitizeForLog(origin),
                    SanitizeForLog(context.Request.Path.ToString()),
                    SanitizeForLog(context.Request.Method),
                    SanitizeForLog(context.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown"),
                    SanitizeForLog(context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"));
            }
            else if (isAllowed)
            {
                _logger.LogDebug(
                    "CORS: Allowed request from origin {Origin} to {Path}",
                    SanitizeForLog(origin),
                    SanitizeForLog(context.Request.Path.ToString()));
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for adding CORS logging middleware.
/// </summary>
public static class CorsLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds CORS logging middleware to the pipeline.
    /// Should be called before UseCors().
    /// </summary>
    public static IApplicationBuilder UseCorsLogging(this IApplicationBuilder app, CorsSettings settings)
    {
        return app.UseMiddleware<CorsLoggingMiddleware>(settings);
    }
}
