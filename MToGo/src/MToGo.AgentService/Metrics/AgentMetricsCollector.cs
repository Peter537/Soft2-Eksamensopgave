using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MToGo.AgentService.Data;
using Prometheus;

namespace MToGo.AgentService.Metrics;

/// <summary>
/// Background service that collects and exposes Agent-related KPI metrics for Prometheus.
/// KPIs:
/// - Active agents: Number of agents currently available to deliver orders (IsActive = true)
/// </summary>
public class AgentMetricsCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentMetricsCollector> _logger;

    // Gauge for active agents (available for deliveries)
    private static readonly Gauge ActiveAgents = Prometheus.Metrics
        .CreateGauge(
            "mtogo_active_agents",
            "Number of agents currently available to deliver orders");

    // Gauge for total agents (not deleted)
    private static readonly Gauge TotalAgents = Prometheus.Metrics
        .CreateGauge(
            "mtogo_total_agents",
            "Total number of registered agents (not deleted)");

    public AgentMetricsCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentMetricsCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentMetricsCollector started");

        // Initial delay to allow database to be ready
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting agent metrics");
            }

            // Collect metrics every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CollectMetricsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();

        // Active agents: Agents with IsActive = true and IsDeleted = false
        var activeAgents = await dbContext.Agents
            .Where(a => a.IsActive && !a.IsDeleted)
            .CountAsync();
        ActiveAgents.Set(activeAgents);

        // Total agents (not deleted)
        var totalAgents = await dbContext.Agents
            .Where(a => !a.IsDeleted)
            .CountAsync();
        TotalAgents.Set(totalAgents);

        _logger.LogDebug(
            "Metrics collected: ActiveAgents={ActiveAgents}, TotalAgents={TotalAgents}",
            activeAgents, totalAgents);
    }
}
