namespace MToGo.Shared.Logging
{
    /// <summary>
    /// Defines the type of log entry for categorization.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// System operational logs (debug, info, warning, error).
        /// </summary>
        System,

        /// <summary>
        /// Audit logs for tracking user actions and business events.
        /// </summary>
        Audit
    }
}
