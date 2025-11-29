using Reqnroll;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class MarkOrderReadyStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public MarkOrderReadyStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"the partner marks the order as ready")]
        public async Task WhenThePartnerMarksTheOrderAsReady()
        {
            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");
            var response = await client.PostAsync($"/orders/order/{orderId}/set-ready", null);
            _scenarioContext["Response"] = response;
        }

        [Then(@"OrderReady kafka event is published")]
        public void ThenOrderReadyKafkaEventIsPublished()
        {
            var kafkaMock = _scenarioContext.Get<Mock<IKafkaProducer>>("KafkaMock");
            kafkaMock.Verify(p => p.PublishAsync(KafkaTopics.OrderReady, It.IsAny<string>(), It.IsAny<OrderReadyEvent>()), Times.Once);
        }
    }
}
