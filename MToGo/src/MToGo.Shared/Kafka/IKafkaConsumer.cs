namespace MToGo.Shared.Kafka
{
    public interface IKafkaConsumer
    {
        Task ConsumeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default);
    }
}
