using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

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

        string sanitized = Regex.Replace(
            value,
            @"[\p{C}\u2028\u2029\u000B\u000C\u0085|]+", 
            ""
        );

        sanitized = sanitized.Replace("\r", "").Replace("\n", "");
        return $"[USERINPUT:{sanitized}]";
    }

    private static string SanitizeHttpMethodForLog(string? method)
    {
        if (string.IsNullOrEmpty(method))
        {
            return "UNKNOWN";
        }

        string[] allowedMethods = new[]
        {
            "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE", "CONNECT"
        };

        string trimmed = method.Trim();
        if (allowedMethods.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {            
            string sanitized = trimmed.ToUpperInvariant().Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("|", "");
            return sanitized;
        }
        return "UNKNOWN";
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
                    SanitizeHttpMethodForLog(context.Request.Method),
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
