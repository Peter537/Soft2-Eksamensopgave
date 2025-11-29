using Reqnroll;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.OrderService.Tests.Fixtures;
using FluentAssertions;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class CompleteDeliveryStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public CompleteDeliveryStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"the agent marks the order as delivered")]
        public async Task WhenTheAgentMarksTheOrderAsDelivered()
        {
            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");

            var response = await client.PostAsync($"/orders/order/{orderId}/complete-delivery", null);
            _scenarioContext["Response"] = response;
        }

        [When(@"the agent marks order (\d+) as delivered")]
        public async Task WhenTheAgentMarksOrderAsDelivered(int orderId)
        {
            var client = _scenarioContext.Get<HttpClient>("Client");

            var response = await client.PostAsync($"/orders/order/{orderId}/complete-delivery", null);
            _scenarioContext["Response"] = response;
        }

        [Then(@"OrderDelivered kafka event is published")]
        public void ThenOrderDeliveredKafkaEventIsPublished()
        {
            var kafkaMock = _scenarioContext.Get<Mock<IKafkaProducer>>("KafkaMock");
            kafkaMock.Verify(
                p => p.PublishAsync(
                    KafkaTopics.OrderDelivered,
                    It.IsAny<string>(),
                    It.IsAny<OrderDeliveredEvent>()),
                Times.Once);
        }
    }
}
