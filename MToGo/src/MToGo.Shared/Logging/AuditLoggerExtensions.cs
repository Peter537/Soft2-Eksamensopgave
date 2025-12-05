using Microsoft.Extensions.Logging;

namespace MToGo.Shared.Logging
{
    /// <summary>
    /// Extension methods for ILogger to support audit logging.
    /// Audit logs are specially marked to be stored in the database for compliance and tracking.
    /// </summary>
    public static class AuditLoggerExtensions
    {
        /// <summary>
        /// Sanitizes a string value to prevent log injection attacks.
        /// Removes or escapes characters that could be used for log forging.
        /// </summary>
        private static string SanitizeLogValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Remove newlines, carriage returns, and tabs that could forge log entries
            // Also escape any potential format string markers
            return value
                .Replace("\r", "")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("{", "{{")
                .Replace("}", "}}");
        }

        /// <summary>
        /// Sanitizes all arguments to prevent log injection attacks.
        /// </summary>
        private static object?[] SanitizeArgs(object?[] args)
        {
            if (args == null || args.Length == 0)
                return args ?? Array.Empty<object?>();

            var sanitized = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                sanitized[i] = args[i] is string strValue ? SanitizeLogValue(strValue) : args[i];
            }
            return sanitized;
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
                // Sanitize user-provided args to prevent log injection
                var sanitizedArgs = SanitizeArgs(args);
                var formattedMessage = $"[AUDIT] {action}: {message}";
                
                switch (level)
                {
                    case LogLevel.Debug:
                        logger.LogDebug(formattedMessage, sanitizedArgs);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(formattedMessage, sanitizedArgs);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(formattedMessage, sanitizedArgs);
                        break;
                    case LogLevel.Error:
                        logger.LogError(exception, formattedMessage, sanitizedArgs);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(exception, formattedMessage, sanitizedArgs);
                        break;
                    default:
                        logger.LogInformation(formattedMessage, sanitizedArgs);
                        break;
                }
            }
        }
    }
}
