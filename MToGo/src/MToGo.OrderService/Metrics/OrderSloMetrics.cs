using Prometheus;

namespace MToGo.OrderService.Metrics;

public static class OrderSloMetrics
{
    private static readonly Counter OrderCreateRequests = Prometheus.Metrics
        .CreateCounter(
            "mtogo_order_create_requests_total",
            "Total number of CreateOrder attempts (success/error)",
            new CounterConfiguration
            {
                LabelNames = ["result"]
            });

    private static readonly Histogram OrderCreationLatencySeconds = Prometheus.Metrics
        .CreateHistogram(
            "mtogo_order_creation_latency_seconds",
            "Latency from CreateOrder start until Kafka publish completed",
            new HistogramConfiguration
            {
                // Buckets tuned for <400ms objective
                Buckets = [0.05, 0.1, 0.2, 0.3, 0.4, 0.5, 0.75, 1, 2, 5]
            });

    public static void RecordCreateOrderSuccess(double latencySeconds)
    {
        OrderCreateRequests.WithLabels("success").Inc();
        OrderCreationLatencySeconds.Observe(latencySeconds);
    }

    public static void RecordCreateOrderError()
    {
        OrderCreateRequests.WithLabels("error").Inc();
    }
}
