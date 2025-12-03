using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace MToGo.Shared.Metrics;

/// <summary>
/// Extension methods for configuring Prometheus metrics in MToGo services.
/// Supports both Docker Compose (local development) and Kubernetes (production) deployments.
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Adds Prometheus metrics collection services.
    /// </summary>
    public static IServiceCollection AddMToGoMetrics(this IServiceCollection services)
    {
        // prometheus-net automatically registers default metrics
        // Additional custom metrics are registered in individual services
        return services;
    }

    /// <summary>
    /// Maps the Prometheus metrics endpoint at /metrics.
    /// This endpoint is scraped by Prometheus for metrics collection.
    /// Works in both Docker Compose and Kubernetes environments.
    /// </summary>
    public static IEndpointRouteBuilder MapMToGoMetrics(this IEndpointRouteBuilder endpoints)
    {
        // Map the /metrics endpoint for Prometheus scraping
        endpoints.MapMetrics();
        return endpoints;
    }

    /// <summary>
    /// Adds HTTP request metrics middleware.
    /// Should be called early in the pipeline to capture all requests.
    /// </summary>
    public static IApplicationBuilder UseMToGoHttpMetrics(this IApplicationBuilder app)
    {
        // Add HTTP request duration metrics
        app.UseHttpMetrics(options =>
        {
            // Include path labels for detailed metrics
            options.AddCustomLabel("service", context => context.Request.Host.Host);
        });
        return app;
    }
}
