using MToGo.Shared.Kafka;
using MToGo.Shared.Logging;

namespace MToGo.LogCollectorService.Services
{
    /// <summary>
    /// Background service that consumes log entries from Kafka and stores them.
    /// </summary>
    public class LogConsumerService : BackgroundService
    {
        private readonly IKafkaConsumer _kafkaConsumer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISystemLogService _systemLogService;
        private readonly ILogger<LogConsumerService> _logger;

        public LogConsumerService(
            IKafkaConsumer kafkaConsumer,
            IServiceScopeFactory scopeFactory,
            ISystemLogService systemLogService,
            ILogger<LogConsumerService> logger)
        {
            _kafkaConsumer = kafkaConsumer;
            _scopeFactory = scopeFactory;
            _systemLogService = systemLogService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LogConsumerService starting...");

            try
            {
                await _kafkaConsumer.ConsumeAsync<LogEntry>(async logEntry =>
                {
                    try
                    {
                        _logger.LogDebug("Processing log entry {LogId} with Type={Type}, Action={Action}, Service={Service}", 
                            logEntry.Id, logEntry.Type, logEntry.Action, logEntry.ServiceName);
                        
                        if (logEntry.Type == LogType.Audit)
                        {
                            _logger.LogInformation("Saving audit log: {LogId} - {Action} on {Resource}", 
                                logEntry.Id, logEntry.Action, logEntry.Resource);
                            // Store audit logs in the database
                            using var scope = _scopeFactory.CreateScope();
                            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                            await auditLogService.SaveAuditLogAsync(logEntry);
                        }
                        else
                        {
                            // Store system logs in rotating files
                            await _systemLogService.WriteLogAsync(logEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process log entry: {LogId}", logEntry.Id);
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("LogConsumerService stopped gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogConsumerService encountered an error");
                throw;
            }
        }
    }
}
