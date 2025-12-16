namespace MToGo.Shared.Logging
{
    /// <summary>
    /// Represents a log entry to be sent to the Kafka app-logs topic.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Unique identifier for the log entry.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp when the log was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Type of log entry (System or Audit).
        /// </summary>
        public LogType Type { get; set; }

        /// <summary>
        /// Log level (Debug, Information, Warning, Error, Critical).
        /// </summary>
        public string Level { get; set; } = "Information";

        /// <summary>
        /// Name of the service that generated the log.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Category or logger name.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception details if applicable.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Additional structured properties.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new();

        /// <summary>
        /// User ID associated with this action (for audit logs).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// User role associated with this action (for audit logs).
        /// </summary>
        public string? UserRole { get; set; }

        /// <summary>
        /// Name of the action being performed (for audit logs).
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Resource or entity being acted upon (for audit logs).
        /// </summary>
        public string? Resource { get; set; }

        /// <summary>
        /// Resource ID being acted upon (for audit logs).
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// Trace ID for distributed tracing.
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// Span ID for distributed tracing.
        /// </summary>
        public string? SpanId { get; set; }

        /// <summary>
        /// Machine/container name where the log was generated.
        /// </summary>
        public string? MachineName { get; set; }
    }
}
