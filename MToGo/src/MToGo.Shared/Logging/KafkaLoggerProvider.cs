using Microsoft.Extensions.Logging;
using MToGo.Shared.Kafka;

namespace MToGo.Shared.Logging
{
    /// <summary>
    /// Logger provider that creates KafkaLogger instances.
    /// Uses lazy initialization to avoid circular dependency with IKafkaProducer.
    /// </summary>
    public class KafkaLoggerProvider : ILoggerProvider
    {
        private readonly string _serviceName;
        private readonly Func<IKafkaProducer> _kafkaProducerFactory;
        private readonly LogLevel _minimumLevel;
        private readonly Dictionary<string, KafkaLogger> _loggers = new();
        private readonly object _lock = new();
        private bool _disposed;
        private IKafkaProducer? _kafkaProducer;

        public KafkaLoggerProvider(string serviceName, Func<IKafkaProducer> kafkaProducerFactory, LogLevel minimumLevel = LogLevel.Information)
        {
            _serviceName = serviceName;
            _kafkaProducerFactory = kafkaProducerFactory;
            _minimumLevel = minimumLevel;
        }

        // Backwards compatibility constructor
        public KafkaLoggerProvider(string serviceName, IKafkaProducer kafkaProducer, LogLevel minimumLevel = LogLevel.Information)
        {
            _serviceName = serviceName;
            _kafkaProducerFactory = () => kafkaProducer;
            _kafkaProducer = kafkaProducer;
            _minimumLevel = minimumLevel;
        }

        internal IKafkaProducer GetKafkaProducer()
        {
            if (_kafkaProducer == null)
            {
                lock (_lock)
                {
                    _kafkaProducer ??= _kafkaProducerFactory();
                }
            }
            return _kafkaProducer;
        }

        public ILogger CreateLogger(string categoryName)
        {
            lock (_lock)
            {
                if (!_loggers.TryGetValue(categoryName, out var logger))
                {
                    logger = new KafkaLogger(categoryName, _serviceName, this, _minimumLevel);
                    _loggers[categoryName] = logger;
                }
                return logger;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _loggers.Clear();
                _disposed = true;
            }
        }
    }
}
