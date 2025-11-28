using System.Threading.Tasks;

namespace MToGo.Shared.Kafka
{
    public interface IKafkaProducer
    {
        Task PublishAsync<T>(string topic, string key, T eventData);
    }
}