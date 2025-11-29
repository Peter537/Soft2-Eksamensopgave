using Reqnroll;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class AcceptOrderStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public AcceptOrderStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"the partner accepts the order")]
        public async Task WhenThePartnerAcceptsTheOrder()
        {
            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");
            var response = await client.PostAsync($"/orders/order/{orderId}/accept", null);
            _scenarioContext["Response"] = response;
        }

        [Then(@"OrderAccepted kafka event is published")]
        public void ThenOrderAcceptedKafkaEventIsPublished()
        {
            var kafkaMock = _scenarioContext.Get<Mock<IKafkaProducer>>("KafkaMock");
            kafkaMock.Verify(p => p.PublishAsync(KafkaTopics.OrderAccepted, It.IsAny<string>(), It.IsAny<OrderAcceptedEvent>()), Times.Once);
        }
    }
}
