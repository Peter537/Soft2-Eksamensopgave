using MToGo.LogCollectorService.Entities;
using MToGo.LogCollectorService.Models;
using MToGo.Shared.Logging;
using System.Text.Json;

namespace MToGo.LogCollectorService.Services
{
    public interface IAuditLogService
    {
        Task SaveAuditLogAsync(LogEntry entry);
        Task<List<AuditLog>> GetAuditLogsAsync(AuditLogQuery query);
        Task<int> GetAuditLogCountAsync(AuditLogQuery query);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly LogDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(LogDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveAuditLogAsync(LogEntry entry)
        {
            var auditLog = new AuditLog
            {
                LogId = entry.Id,
                Timestamp = entry.Timestamp,
                Level = entry.Level,
                ServiceName = entry.ServiceName,
                Category = entry.Category,
                Message = entry.Message,
                UserId = entry.UserId,
                UserRole = entry.UserRole,
                Action = entry.Action,
                Resource = entry.Resource,
                ResourceId = entry.ResourceId,
                TraceId = entry.TraceId,
                MachineName = entry.MachineName,
                PropertiesJson = entry.Properties.Count > 0
                    ? JsonSerializer.Serialize(entry.Properties)
                    : null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Saved audit log: {LogId} - {Action} on {Resource}", entry.Id, entry.Action, entry.Resource);
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(AuditLogQuery query)
        {
            var queryable = BuildQuery(query);

            var logs = await Task.Run(() =>
                queryable
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList());

            return logs;
        }

        public async Task<int> GetAuditLogCountAsync(AuditLogQuery query)
        {
            var queryable = BuildQuery(query);
            return await Task.Run(() => queryable.Count());
        }

        private IQueryable<AuditLog> BuildQuery(AuditLogQuery query)
        {
            var queryable = _context.AuditLogs.AsQueryable();

            if (query.StartDate.HasValue)
            {
                var startDateUtc = DateTime.SpecifyKind(query.StartDate.Value.Date, DateTimeKind.Utc);
                queryable = queryable.Where(l => l.Timestamp >= startDateUtc);
            }

            if (query.EndDate.HasValue)
            {
                // Include the entire end date by going to the start of the next day
                var endDateUtc = DateTime.SpecifyKind(query.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                queryable = queryable.Where(l => l.Timestamp < endDateUtc);
            }

            if (!string.IsNullOrEmpty(query.ServiceName))
                queryable = queryable.Where(l => l.ServiceName == query.ServiceName);

            if (!string.IsNullOrEmpty(query.Level))
                queryable = queryable.Where(l => l.Level == query.Level);

            if (!string.IsNullOrEmpty(query.Action))
                queryable = queryable.Where(l => l.Action != null && l.Action.Contains(query.Action));

            if (!string.IsNullOrEmpty(query.Resource))
                queryable = queryable.Where(l => l.Resource == query.Resource);

            if (query.UserId.HasValue)
                queryable = queryable.Where(l => l.UserId == query.UserId);

            if (!string.IsNullOrEmpty(query.SearchText))
                queryable = queryable.Where(l => l.Message.Contains(query.SearchText));

            return queryable;
        }
    }
}
