using MToGo.LogCollectorService.Models;
using MToGo.Shared.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace MToGo.LogCollectorService.Services
{
    public interface ISystemLogService
    {
        Task WriteLogAsync(LogEntry entry);
        List<string> GetLogFiles();
        Task<string> ReadLogFileAsync(string filename);
        Task<SystemLogFileInfo?> GetLogFileInfoAsync(string filename);
    }

    public class SystemLogService : ISystemLogService
    {
        private readonly string _logsDirectory;
        private readonly ILogger<SystemLogService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();
        private readonly JsonSerializerOptions _jsonOptions;

        public SystemLogService(IConfiguration configuration, ILogger<SystemLogService> logger)
        {
            _logsDirectory = configuration["Logging:SystemLogsDirectory"] ?? "/var/log/mtogo";
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Directory.CreateDirectory(_logsDirectory);
        }

        public async Task WriteLogAsync(LogEntry entry)
        {
            var filename = $"system-{entry.Timestamp:yyyy-MM-dd}.log";
            var filepath = Path.Combine(_logsDirectory, filename);

            var fileLock = _fileLocks.GetOrAdd(filename, _ => new SemaphoreSlim(1, 1));

            await fileLock.WaitAsync();
            try
            {
                var logLine = FormatLogLine(entry);
                await File.AppendAllTextAsync(filepath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write system log to file: {Filename}", filename);
            }
            finally
            {
                fileLock.Release();
            }
        }

        public List<string> GetLogFiles()
        {
            if (!Directory.Exists(_logsDirectory))
                return new List<string>();

            return Directory.GetFiles(_logsDirectory, "system-*.log")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .OrderByDescending(f => f)
                .ToList();
        }

        public async Task<string> ReadLogFileAsync(string filename)
        {
            // Validate filename to prevent directory traversal
            if (filename.Contains("..") || filename.Contains(Path.DirectorySeparatorChar) || filename.Contains(Path.AltDirectorySeparatorChar))
                throw new ArgumentException("Invalid filename");

            if (!filename.StartsWith("system-") || !filename.EndsWith(".log"))
                throw new ArgumentException("Invalid log filename format");

            var filepath = Path.Combine(_logsDirectory, filename);

            if (!File.Exists(filepath))
                throw new FileNotFoundException($"Log file not found: {filename}");

            return await File.ReadAllTextAsync(filepath, Encoding.UTF8);
        }

        public async Task<SystemLogFileInfo?> GetLogFileInfoAsync(string filename)
        {
            // Validate filename
            if (filename.Contains("..") || filename.Contains(Path.DirectorySeparatorChar) || filename.Contains(Path.AltDirectorySeparatorChar))
                return null;

            if (!filename.StartsWith("system-") || !filename.EndsWith(".log"))
                return null;

            var filepath = Path.Combine(_logsDirectory, filename);

            if (!File.Exists(filepath))
                return null;

            var fileInfo = new FileInfo(filepath);
            var lines = await File.ReadAllLinesAsync(filepath);

            // Parse date from filename (system-yyyy-MM-dd.log)
            var dateStr = filename.Replace("system-", "").Replace(".log", "");
            DateTime.TryParse(dateStr, out var date);

            return new SystemLogFileInfo
            {
                Date = dateStr,
                FileName = filename,
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTimeUtc
            };
        }

        private string FormatLogLine(LogEntry entry)
        {
            var sb = new StringBuilder();
            sb.Append($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{entry.Level,-11}] ");
            sb.Append($"[{entry.ServiceName}] ");
            sb.Append($"[{entry.Category}] ");
            sb.Append(entry.Message);

            if (!string.IsNullOrEmpty(entry.Exception))
            {
                sb.AppendLine();
                sb.Append("  Exception: ");
                sb.Append(entry.Exception);
            }

            if (entry.Properties.Count > 0)
            {
                sb.Append(" | Props: ");
                sb.Append(JsonSerializer.Serialize(entry.Properties, _jsonOptions));
            }

            return sb.ToString();
        }
    }
}
