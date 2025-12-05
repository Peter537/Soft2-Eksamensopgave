using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace MToGo.Shared.Logging
{
    /// <summary>
    /// Extension methods for ILogger to support audit logging.
    /// Audit logs are specially marked to be stored in the database for compliance and tracking.
    /// </summary>
    public static partial class AuditLoggerExtensions
    {
        /// <summary>
        /// Regex to match format placeholders like {0}, {Name}, {PartnerId}, etc.
        /// </summary>
        [GeneratedRegex(@"\{[^}]+\}")]
        private static partial Regex FormatPlaceholderRegex();

        /// <summary>
        /// Sanitizes a string value to prevent log injection attacks.
        /// Removes all control characters (Unicode category Cc).
        /// </summary>
        private static string SanitizeForLog(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Remove all control characters to prevent log forging
            return new string(value.Where(c => !char.IsControl(c)).ToArray());
        }

        /// <summary>
        /// Formats a message template with sanitized arguments.
        /// This pre-formats the message to avoid passing user input directly to logger methods.
        /// </summary>
        private static string FormatMessageSafely(string messageTemplate, object?[] args)
        {
            if (args == null || args.Length == 0)
                return messageTemplate;

            // Find all placeholders and replace them with sanitized argument values
            var result = messageTemplate;
            var matches = FormatPlaceholderRegex().Matches(messageTemplate);
            
            for (int i = 0; i < matches.Count && i < args.Length; i++)
            {
                var placeholder = matches[i].Value;
                var argValue = args[i]?.ToString() ?? string.Empty;
                var sanitizedValue = SanitizeForLog(argValue);
                result = result.Replace(placeholder, sanitizedValue);
            }

            return result;
        }

        /// <summary>
        /// Logs an audit entry with Debug level.
        /// </summary>
        public static void LogAuditDebug(this ILogger logger, string action, string resource, string? resourceId, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Debug, action, resource, resourceId, null, null, message, null, args);
        }

        /// <summary>
        /// Logs an audit entry with Information level.
        /// </summary>
        public static void LogAuditInformation(this ILogger logger, string action, string resource, string? resourceId, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Information, action, resource, resourceId, null, null, message, null, args);
        }

        /// <summary>
        /// Logs an audit entry with Warning level.
        /// </summary>
        public static void LogAuditWarning(this ILogger logger, string action, string resource, string? resourceId, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Warning, action, resource, resourceId, null, null, message, null, args);
        }

        /// <summary>
        /// Logs an audit entry with Error level.
        /// </summary>
        public static void LogAuditError(this ILogger logger, Exception? exception, string action, string resource, string? resourceId, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Error, action, resource, resourceId, null, null, message, exception, args);
        }

        /// <summary>
        /// Logs an audit entry with Information level including user context.
        /// </summary>
        public static void LogAuditInformation(this ILogger logger, string action, string resource, string? resourceId, int? userId, string? userRole, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Information, action, resource, resourceId, userId, userRole, message, null, args);
        }

        /// <summary>
        /// Logs an audit entry with Warning level including user context.
        /// </summary>
        public static void LogAuditWarning(this ILogger logger, string action, string resource, string? resourceId, int? userId, string? userRole, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Warning, action, resource, resourceId, userId, userRole, message, null, args);
        }

        /// <summary>
        /// Logs an audit entry with Error level including user context.
        /// </summary>
        public static void LogAuditError(this ILogger logger, Exception? exception, string action, string resource, string? resourceId, int? userId, string? userRole, string message, params object?[] args)
        {
            LogAuditInternal(logger, LogLevel.Error, action, resource, resourceId, userId, userRole, message, exception, args);
        }

        private static void LogAuditInternal(
            ILogger logger,
            LogLevel level,
            string action,
            string resource,
            string? resourceId,
            int? userId,
            string? userRole,
            string message,
            Exception? exception,
            params object?[] args)
        {
            // Pre-format the message with sanitized arguments to break taint tracking
            // This ensures no user-controlled data flows directly to logger format args
            var safeMessage = FormatMessageSafely(message, args);
            var fullMessage = $"[AUDIT] {action}: {safeMessage}";

            // Create a structured log with audit metadata
            using (logger.BeginScope(new Dictionary<string, object?>
            {
                ["LogType"] = LogType.Audit.ToString(),
                ["AuditAction"] = action,
                ["AuditResource"] = resource,
                ["AuditResourceId"] = resourceId,
                ["AuditUserId"] = userId,
                ["AuditUserRole"] = userRole
            }))
            {
                // Log with no format arguments - message is already fully formatted and sanitized
                switch (level)
                {
                    case LogLevel.Debug:
                        logger.LogDebug("{Message}", fullMessage);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation("{Message}", fullMessage);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning("{Message}", fullMessage);
                        break;
                    case LogLevel.Error:
                        logger.LogError(exception, "{Message}", fullMessage);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(exception, "{Message}", fullMessage);
                        break;
                    default:
                        logger.LogInformation("{Message}", fullMessage);
                        break;
                }
            }
        }
    }
}
