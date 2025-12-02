using Reqnroll;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.OrderService.Tests.Fixtures;
using FluentAssertions;
using MToGo.Testing;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class PickUpOrderStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public PickUpOrderStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"the agent confirms pickup for the order")]
        public async Task WhenTheAgentConfirmsPickupForTheOrder()
        {
            // Set up Agent role for this action
            TestAuthenticationHandler.SetTestUser("1", "Agent");

            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");

            var response = await client.PostAsync($"/orders/order/{orderId}/pickup", null);
            _scenarioContext["Response"] = response;
        }

        [When(@"the agent confirms pickup for order (\d+)")]
        public async Task WhenTheAgentConfirmsPickupForOrder(int orderId)
        {
            // Set up Agent role for this action
            TestAuthenticationHandler.SetTestUser("1", "Agent");

            var client = _scenarioContext.Get<HttpClient>("Client");

            var response = await client.PostAsync($"/orders/order/{orderId}/pickup", null);
            _scenarioContext["Response"] = response;
        }

        [Then(@"OrderPickedUp kafka event is published with the agent name")]
        public void ThenOrderPickedUpKafkaEventIsPublishedWithTheAgentName()
        {
            var kafkaMock = _scenarioContext.Get<Mock<IKafkaProducer>>("KafkaMock");
            kafkaMock.Verify(
                p => p.PublishAsync(
                    KafkaTopics.OrderPickedUp, 
                    It.IsAny<string>(), 
                    It.Is<OrderPickedUpEvent>(e => !string.IsNullOrEmpty(e.AgentName))), 
                Times.Once);
        }
    }
}
