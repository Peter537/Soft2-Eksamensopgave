using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MToGo.PartnerService.Data;
using Prometheus;

namespace MToGo.PartnerService.Metrics;

/// <summary>
/// Background service that collects and exposes Partner-related KPI metrics for Prometheus.
/// KPIs:
/// - Active partners: Number of partners currently activated to receive orders (IsActive = true)
/// </summary>
public class PartnerMetricsCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PartnerMetricsCollector> _logger;

    // Gauge for active partners
    private static readonly Gauge ActivePartners = Prometheus.Metrics
        .CreateGauge(
            "mtogo_active_partners",
            "Number of partners currently activated to receive orders");

    // Gauge for total partners (not deleted)
    private static readonly Gauge TotalPartners = Prometheus.Metrics
        .CreateGauge(
            "mtogo_total_partners",
            "Total number of registered partners (not deleted)");

    public PartnerMetricsCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<PartnerMetricsCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PartnerMetricsCollector started");

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
                _logger.LogError(ex, "Error collecting partner metrics");
            }

            // Collect metrics every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CollectMetricsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();

        // Active partners: Partners with IsActive = true and IsDeleted = false
        var activePartners = await dbContext.Partners
            .Where(p => p.IsActive && !p.IsDeleted)
            .CountAsync();
        ActivePartners.Set(activePartners);

        // Total partners (not deleted)
        var totalPartners = await dbContext.Partners
            .Where(p => !p.IsDeleted)
            .CountAsync();
        TotalPartners.Set(totalPartners);

        _logger.LogDebug(
            "Metrics collected: ActivePartners={ActivePartners}, TotalPartners={TotalPartners}",
            activePartners, totalPartners);
    }
}
