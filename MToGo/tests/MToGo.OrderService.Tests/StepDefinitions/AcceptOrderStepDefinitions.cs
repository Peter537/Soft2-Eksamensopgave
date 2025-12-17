using Reqnroll;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.Testing;
using System.Text;
using System.Text.Json;

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
            // Set up Partner role for this action
            TestAuthenticationHandler.SetTestUser("1", "Partner");

            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");
            var content = new StringContent(
                JsonSerializer.Serialize(new { estimatedMinutes = 15 }),
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync($"/orders/order/{orderId}/accept", content);
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

