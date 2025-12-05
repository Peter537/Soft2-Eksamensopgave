namespace MToGo.LogCollectorService.Models
{
    public class AuditLogQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ServiceName { get; set; }
        public string? Level { get; set; }
        public string? Action { get; set; }
        public string? Resource { get; set; }
        public int? UserId { get; set; }
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
