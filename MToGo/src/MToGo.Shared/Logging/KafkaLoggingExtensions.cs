using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MToGo.Shared.Kafka;

namespace MToGo.Shared.Logging
{
    /// <summary>
    /// Extension methods for configuring Kafka-based logging.
    /// </summary>
    public static class KafkaLoggingExtensions
    {
        /// <summary>
        /// Adds Kafka logging provider to send logs to the centralized log collector.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="serviceName">Name of the service for log categorization.</param>
        /// <param name="minimumLevel">Minimum log level to send to Kafka.</param>
        /// <returns>The logging builder for chaining.</returns>
        public static ILoggingBuilder AddKafkaLogger(this ILoggingBuilder builder, string serviceName, LogLevel minimumLevel = LogLevel.Information)
        {
            builder.Services.AddSingleton<ILoggerProvider>(sp =>
            {
                // Use a factory function to lazily resolve the Kafka producer
                // This avoids circular dependency during service construction
                return new KafkaLoggerProvider(serviceName, () => sp.GetRequiredService<IKafkaProducer>(), minimumLevel);
            });

            return builder;
        }

        /// <summary>
        /// Adds Kafka logging to the service collection with the specified configuration.
        /// </summary>
        public static IServiceCollection AddKafkaLogging(this IServiceCollection services, string serviceName, LogLevel minimumLevel = LogLevel.Information)
        {
            services.AddSingleton<ILoggerProvider>(sp =>
            {
                // Use a factory function to lazily resolve the Kafka producer
                // This avoids circular dependency during service construction
                return new KafkaLoggerProvider(serviceName, () => sp.GetRequiredService<IKafkaProducer>(), minimumLevel);
            });

            return services;
        }
    }
}
