namespace MToGo.Shared.Security.Cors;

/// <summary>
/// Configuration settings for CORS policies.
/// </summary>
public class CorsSettings
{
    public const string SectionName = "CorsSettings";

    /// <summary>
    /// List of allowed origins for cross-origin requests.
    /// These should be the full URLs (e.g., "http://localhost:8081", "https://mtogo.example.com").
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to allow credentials (cookies, authorization headers) in CORS requests.
    /// Required for WebSocket connections that use authentication.
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// List of allowed HTTP methods. If empty, defaults to common methods.
    /// </summary>
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of allowed headers. If empty, allows all headers.
    /// </summary>
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of headers exposed to the client.
    /// </summary>
    public string[] ExposedHeaders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// How long (in seconds) the browser should cache the preflight response.
    /// </summary>
    public int PreflightMaxAgeSeconds { get; set; } = 600;

    /// <summary>
    /// Whether to log blocked CORS requests.
    /// </summary>
    public bool LogBlockedRequests { get; set; } = true;
}
