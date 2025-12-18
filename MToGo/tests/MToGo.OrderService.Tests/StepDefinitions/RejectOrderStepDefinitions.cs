using Reqnroll;
using System.Net.Http.Json;
using MToGo.OrderService.Models;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.Testing;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class RejectOrderStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public RejectOrderStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"the partner rejects the order without reason")]
        public async Task WhenThePartnerRejectsTheOrderWithoutReason()
        {
            // Set up Partner role for this action
            TestAuthenticationHandler.SetTestUser("1", "Partner");

            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");
            var response = await client.PostAsync($"/orders/order/{orderId}/reject", null);
            _scenarioContext["Response"] = response;
        }

        [When(@"the partner rejects the order with reason ""(.*)""")]
        public async Task WhenThePartnerRejectsTheOrderWithReason(string reason)
        {
            // Set up Partner role for this action
            TestAuthenticationHandler.SetTestUser("1", "Partner");

            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");
            var request = new OrderRejectRequest { Reason = reason };
            var response = await client.PostAsJsonAsync($"/orders/order/{orderId}/reject", request);
            _scenarioContext["Response"] = response;
        }

        [Then(@"OrderRejected kafka event is published")]
        public void ThenOrderRejectedKafkaEventIsPublished()
        {
            var kafkaMock = _scenarioContext.Get<Mock<IKafkaProducer>>("KafkaMock");
            kafkaMock.Verify(p => p.PublishAsync(KafkaTopics.OrderRejected, It.IsAny<string>(), It.IsAny<OrderRejectedEvent>()), Times.Once);
        }
    }
}

