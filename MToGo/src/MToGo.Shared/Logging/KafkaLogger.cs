using Microsoft.Extensions.Logging;
using MToGo.Shared.Kafka;
using System.Diagnostics;

namespace MToGo.Shared.Logging
{
    /// <summary>
    /// A custom logger that sends log entries to Kafka for centralized logging.
    /// Uses lazy initialization of KafkaProducer to avoid circular dependency.
    /// </summary>
    public class KafkaLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _serviceName;
        private readonly KafkaLoggerProvider _provider;
        private readonly LogLevel _minimumLevel;

        // Categories to exclude to avoid circular logging (Kafka logging itself)
        private static readonly HashSet<string> _excludedPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Confluent.Kafka",
            "MToGo.Shared.Kafka",
            "MToGo.Shared.Logging"
        };

        public KafkaLogger(string categoryName, string serviceName, KafkaLoggerProvider provider, LogLevel minimumLevel)
        {
            _categoryName = categoryName;
            _serviceName = serviceName;
            _provider = provider;
            _minimumLevel = minimumLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new LoggerScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Exclude Kafka-related categories to prevent circular logging
            if (_excludedPrefixes.Any(prefix => _categoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return logLevel >= _minimumLevel && logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);

            // Extract scope properties
            var properties = new Dictionary<string, object?>();
            var logType = LogType.System;
            string? action = null;
            string? resource = null;
            string? resourceId = null;
            int? userId = null;
            string? userRole = null;

            if (state is IReadOnlyList<KeyValuePair<string, object?>> stateProperties)
            {
                foreach (var prop in stateProperties)
                {
                    if (prop.Key != "{OriginalFormat}")
                    {
                        properties[prop.Key] = prop.Value;
                    }
                }
            }

            // Check scope for audit metadata
            var scope = LoggerScope.Current;
            while (scope != null)
            {
                // Handle both nullable and non-nullable dictionary types
                IEnumerable<KeyValuePair<string, object?>>? scopeProperties = null;
                
                if (scope.State is IDictionary<string, object?> nullableDict)
                {
                    scopeProperties = nullableDict;
                }
                else if (scope.State is IDictionary<string, object> nonNullableDict)
                {
                    scopeProperties = nonNullableDict.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value));
                }
                else if (scope.State is IReadOnlyDictionary<string, object?> readOnlyNullableDict)
                {
                    scopeProperties = readOnlyNullableDict;
                }
                else if (scope.State is IReadOnlyDictionary<string, object> readOnlyDict)
                {
                    scopeProperties = readOnlyDict.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value));
                }
                
                if (scopeProperties != null)
                {
                    foreach (var kvp in scopeProperties)
                    {
                        switch (kvp.Key)
                        {
                            case "LogType":
                                if (Enum.TryParse<LogType>(kvp.Value?.ToString(), out var lt))
                                    logType = lt;
                                break;
                            case "AuditAction":
                                action = kvp.Value?.ToString();
                                break;
                            case "AuditResource":
                                resource = kvp.Value?.ToString();
                                break;
                            case "AuditResourceId":
                                resourceId = kvp.Value?.ToString();
                                break;
                            case "AuditUserId":
                                if (kvp.Value is int uid)
                                    userId = uid;
                                break;
                            case "AuditUserRole":
                                userRole = kvp.Value?.ToString();
                                break;
                            default:
                                if (!properties.ContainsKey(kvp.Key))
                                    properties[kvp.Key] = kvp.Value;
                                break;
                        }
                    }
                }
                scope = scope.Parent;
            }

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Type = logType,
                Level = logLevel.ToString(),
                ServiceName = _serviceName,
                Category = _categoryName,
                Message = message,
                Exception = exception?.ToString(),
                Properties = properties,
                UserId = userId,
                UserRole = userRole,
                Action = action,
                Resource = resource,
                ResourceId = resourceId,
                TraceId = Activity.Current?.TraceId.ToString(),
                SpanId = Activity.Current?.SpanId.ToString(),
                MachineName = Environment.MachineName
            };

            // Fire and forget - don't block the logging call
            // Use lazy producer resolution to avoid circular dependency during startup
            _ = Task.Run(async () =>
            {
                try
                {
                    var producer = _provider.GetKafkaProducer();
                    await producer.PublishAsync(KafkaTopics.AppLogs, logEntry.Id, logEntry);
                }
                catch
                {
                    // Swallow exceptions to prevent logging from crashing the app
                }
            });
        }
    }

    /// <summary>
    /// Scope implementation for structured logging.
    /// Uses object type to work with any state type from BeginScope.
    /// </summary>
    internal class LoggerScope : IDisposable
    {
        private static readonly AsyncLocal<LoggerScope?> _current = new();

        public object? State { get; }
        public LoggerScope? Parent { get; }

        public static LoggerScope? Current
        {
            get => _current.Value;
            private set => _current.Value = value;
        }

        public LoggerScope(object? state)
        {
            State = state;
            Parent = Current;
            Current = this;
        }

        public void Dispose()
        {
            Current = Parent;
        }
    }
}
