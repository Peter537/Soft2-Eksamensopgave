namespace MToGo.Shared.Security.Cors;

/// <summary>
/// Constants for CORS policy names used across the application.
/// </summary>
public static class CorsPolicies
{
    /// <summary>
    /// Policy for allowing requests from trusted frontend origins.
    /// This is the main policy used for API endpoints.
    /// </summary>
    public const string TrustedOrigins = "TrustedOrigins";

    /// <summary>
    /// Policy specifically for WebSocket connections.
    /// Allows credentials and specific headers needed for WebSocket handshake.
    /// </summary>
    public const string WebSocketPolicy = "WebSocketPolicy";
}
