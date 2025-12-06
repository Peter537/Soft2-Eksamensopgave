using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.LogCollectorService.Models;
using MToGo.LogCollectorService.Services;
using MToGo.Shared.Security.Authorization;

namespace MToGo.LogCollectorService.Controllers
{
    [ApiController]
    [Route("api/v1/logs")]
    [Authorize(Policy = AuthorizationPolicies.ManagementOnly)]
    public class LogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<LogsController> _logger;

        public LogsController(
            IAuditLogService auditLogService,
            ISystemLogService systemLogService,
            ILogger<LogsController> logger)
        {
            _auditLogService = auditLogService;
            _systemLogService = systemLogService;
            _logger = logger;
        }

        /// <summary>
        /// Get audit logs with optional filtering.
        /// </summary>
        [HttpGet("audit")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? serviceName,
            [FromQuery] string? level,
            [FromQuery] string? action,
            [FromQuery] string? resource,
            [FromQuery] string? userId,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            int? userIdInt = null;
            if (int.TryParse(userId, out var parsed))
                userIdInt = parsed;

            var query = new AuditLogQuery
            {
                StartDate = fromDate,
                EndDate = toDate,
                ServiceName = serviceName,
                Level = level,
                Action = action,
                Resource = resource,
                UserId = userIdInt,
                SearchText = search,
                Page = page,
                PageSize = Math.Min(pageSize, 100) // Cap at 100
            };

            var logs = await _auditLogService.GetAuditLogsAsync(query);
            var totalCount = await _auditLogService.GetAuditLogCountAsync(query);

            return Ok(new
            {
                Logs = logs,
                Page = page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            });
        }

        /// <summary>
        /// Get list of available system log files with info.
        /// </summary>
        [HttpGet("system/files")]
        public async Task<IActionResult> GetSystemLogFiles()
        {
            var files = _systemLogService.GetLogFiles();
            var fileInfos = new List<SystemLogFileInfo>();

            foreach (var file in files)
            {
                var info = await _systemLogService.GetLogFileInfoAsync(file);
                if (info != null)
                    fileInfos.Add(info);
            }

            return Ok(fileInfos);
        }

        /// <summary>
        /// Get contents of a specific system log file by date.
        /// </summary>
        [HttpGet("system/files/{date}")]
        public async Task<IActionResult> GetSystemLogFile(string date)
        {
            try
            {
                // Construct filename from date (system-yyyy-MM-dd.log)
                var filename = $"system-{date}.log";
                var content = await _systemLogService.ReadLogFileAsync(filename);
                return Content(content, "text/plain");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"Log file not found for date: {date}");
            }
        }

        /// <summary>
        /// Get distinct service names from audit logs.
        /// </summary>
        [HttpGet("audit/services")]
        public async Task<IActionResult> GetDistinctServices()
        {
            var logs = await _auditLogService.GetAuditLogsAsync(new AuditLogQuery { PageSize = 10000 });
            var services = logs.Select(l => l.ServiceName).Distinct().OrderBy(s => s).ToList();
            return Ok(services);
        }

        /// <summary>
        /// Get distinct actions from audit logs.
        /// </summary>
        [HttpGet("audit/actions")]
        public async Task<IActionResult> GetDistinctActions()
        {
            var logs = await _auditLogService.GetAuditLogsAsync(new AuditLogQuery { PageSize = 10000 });
            var actions = logs.Where(l => l.Action != null).Select(l => l.Action!).Distinct().OrderBy(a => a).ToList();
            return Ok(actions);
        }
    }
}
