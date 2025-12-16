using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MToGo.OrderService.Entities;
using Prometheus;

namespace MToGo.OrderService.Metrics;

/// <summary>
/// Background service that collects and exposes Order-related KPI metrics for Prometheus.
/// KPIs:
/// - Active customers (monthly): Unique customers with at least one completed order in last 30 days
/// - Orders per hour/day: Order counts for demand and peak time analysis
/// Also tracks previous period values for trend comparison alerts.
/// </summary>
public class OrderMetricsCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderMetricsCollector> _logger;

    // Gauge for active customers in the last 30 days
    private static readonly Gauge ActiveCustomersMonthly = Prometheus.Metrics
        .CreateGauge(
            "mtogo_active_customers_monthly",
            "Number of unique customers who have completed at least one order in the last 30 days");

    // Gauge for active customers in the previous 30-day period (days 31-60 ago)
    private static readonly Gauge ActiveCustomersPreviousMonth = Prometheus.Metrics
        .CreateGauge(
            "mtogo_active_customers_previous_month",
            "Number of unique customers who completed orders in the previous 30-day period (31-60 days ago)");

    // Gauge for total orders in the last hour
    private static readonly Gauge OrdersLastHour = Prometheus.Metrics
        .CreateGauge(
            "mtogo_orders_last_hour",
            "Number of orders placed in the last hour");

    // Gauge for total orders in the last 24 hours
    private static readonly Gauge OrdersLastDay = Prometheus.Metrics
        .CreateGauge(
            "mtogo_orders_last_day",
            "Number of orders placed in the last 24 hours");

    // Gauge for orders on the same day last week (for week-over-week comparison)
    private static readonly Gauge OrdersSameDayLastWeek = Prometheus.Metrics
        .CreateGauge(
            "mtogo_orders_same_day_last_week",
            "Number of orders placed on the same day of week, one week ago");

    // Counter for total orders (incremented on each new order)
    private static readonly Counter TotalOrdersCreated = Prometheus.Metrics
        .CreateCounter(
            "mtogo_orders_created_total",
            "Total number of orders created since service start");

    // Histogram for order values
    private static readonly Histogram OrderValueHistogram = Prometheus.Metrics
        .CreateHistogram(
            "mtogo_order_value_dkk",
            "Distribution of order values in DKK",
            new HistogramConfiguration
            {
                Buckets = new double[] { 50, 100, 150, 200, 300, 500, 750, 1000, 1500, 2000 }
            });

    public OrderMetricsCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderMetricsCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderMetricsCollector started");

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
                _logger.LogError(ex, "Error collecting order metrics");
            }

            // Collect metrics every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task CollectMetricsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);
        var oneHourAgo = now.AddHours(-1);
        var oneDayAgo = now.AddDays(-1);
        var oneWeekAgo = now.AddDays(-7);
        var oneWeekAndOneDayAgo = now.AddDays(-8);

        // Active customers (monthly): Unique customers with completed orders in last 30 days
        var activeCustomers = await dbContext.Orders
            .Where(o => o.CreatedAt >= thirtyDaysAgo && o.Status == OrderStatus.Delivered)
            .Select(o => o.CustomerId)
            .Distinct()
            .CountAsync();
        ActiveCustomersMonthly.Set(activeCustomers);

        // Active customers (previous month): Unique customers with completed orders 31-60 days ago
        var activeCustomersPrevious = await dbContext.Orders
            .Where(o => o.CreatedAt >= sixtyDaysAgo && o.CreatedAt < thirtyDaysAgo && o.Status == OrderStatus.Delivered)
            .Select(o => o.CustomerId)
            .Distinct()
            .CountAsync();
        ActiveCustomersPreviousMonth.Set(activeCustomersPrevious);

        // Orders in the last hour
        var ordersLastHour = await dbContext.Orders
            .Where(o => o.CreatedAt >= oneHourAgo)
            .CountAsync();
        OrdersLastHour.Set(ordersLastHour);

        // Orders in the last 24 hours
        var ordersLastDay = await dbContext.Orders
            .Where(o => o.CreatedAt >= oneDayAgo)
            .CountAsync();
        OrdersLastDay.Set(ordersLastDay);

        // Orders on the same day last week (24-hour window from 7 days ago)
        var ordersSameDayLastWeek = await dbContext.Orders
            .Where(o => o.CreatedAt >= oneWeekAndOneDayAgo && o.CreatedAt < oneWeekAgo)
            .CountAsync();
        OrdersSameDayLastWeek.Set(ordersSameDayLastWeek);

        _logger.LogDebug(
            "Metrics collected: ActiveCustomers={ActiveCustomers}, ActiveCustomersPrevious={ActiveCustomersPrevious}, OrdersLastHour={OrdersLastHour}, OrdersLastDay={OrdersLastDay}, OrdersSameDayLastWeek={OrdersSameDayLastWeek}",
            activeCustomers, activeCustomersPrevious, ordersLastHour, ordersLastDay, ordersSameDayLastWeek);
    }

    /// <summary>
    /// Call this method when a new order is created to increment the counter and record the value.
    /// </summary>
    public static void RecordOrderCreated(decimal orderValue)
    {
        TotalOrdersCreated.Inc();
        OrderValueHistogram.Observe((double)orderValue);
    }
}
