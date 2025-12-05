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
                var formattedMessage = $"[AUDIT] {action}: {message}";
                
                switch (level)
                {
                    case LogLevel.Debug:
                        logger.LogDebug(formattedMessage, args);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(formattedMessage, args);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(formattedMessage, args);
                        break;
                    case LogLevel.Error:
                        logger.LogError(exception, formattedMessage, args);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(exception, formattedMessage, args);
                        break;
                    default:
                        logger.LogInformation(formattedMessage, args);
                        break;
                }
            }
        }
    }
}
