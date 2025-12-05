using System.ComponentModel.DataAnnotations;

namespace MToGo.LogCollectorService.Entities
{
    /// <summary>
    /// Represents an audit log entry stored in the database.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Original log entry ID from the source service.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string LogId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the log was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Log level (Debug, Information, Warning, Error, Critical).
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = "Information";

        /// <summary>
        /// Name of the service that generated the log.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Category or logger name.
        /// </summary>
        [MaxLength(200)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The log message.
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// User ID associated with this action.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// User role associated with this action.
        /// </summary>
        [MaxLength(50)]
        public string? UserRole { get; set; }

        /// <summary>
        /// Name of the action being performed.
        /// </summary>
        [MaxLength(100)]
        public string? Action { get; set; }

        /// <summary>
        /// Resource or entity being acted upon.
        /// </summary>
        [MaxLength(100)]
        public string? Resource { get; set; }

        /// <summary>
        /// Resource ID being acted upon.
        /// </summary>
        [MaxLength(100)]
        public string? ResourceId { get; set; }

        /// <summary>
        /// Trace ID for distributed tracing.
        /// </summary>
        [MaxLength(50)]
        public string? TraceId { get; set; }

        /// <summary>
        /// Additional structured properties as JSON.
        /// </summary>
        public string? PropertiesJson { get; set; }

        /// <summary>
        /// Machine/container name where the log was generated.
        /// </summary>
        [MaxLength(100)]
        public string? MachineName { get; set; }
    }
}
