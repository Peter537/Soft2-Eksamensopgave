using Testcontainers.Kafka;

namespace MToGo.Testing;

public static class KafkaContainerHelper
{
    public static KafkaContainer CreateKafkaContainer()
    {
        return new KafkaBuilder()
            .WithImage("apache/kafka:3.7.0")
            .WithPortBinding(9092, true)
            .Build();
    }
}

