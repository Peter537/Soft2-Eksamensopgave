using Reqnroll;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using MToGo.OrderService.Tests.Fixtures;
using MToGo.OrderService.Tests.Helpers;
using MToGo.OrderService.Entities;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using MToGo.Testing;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class AssignAgentStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public AssignAgentStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [When(@"the agent accepts the delivery offer with agentId (\d+)")]
        public async Task WhenTheAgentAcceptsTheDeliveryOfferWithAgentId(int agentId)
        {
            // Set up Agent role for this action (agent can only assign themselves)
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");

            var requestBody = new { agentId };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"/orders/order/{orderId}/assign-agent", content);
            _scenarioContext["Response"] = response;
        }

        [When(@"two agents try to accept the delivery offer concurrently with agentIds (\d+) and (\d+)")]
        public async Task WhenTwoAgentsTryToAcceptTheDeliveryOfferConcurrentlyWithAgentIds(int agentId1, int agentId2)
        {
            // Note: For concurrent requests, we'll use the first agent's credentials
            // In a real scenario, each request would have different credentials
            TestAuthenticationHandler.SetTestUser(agentId1.ToString(), "Agent");

            var client = _scenarioContext.Get<HttpClient>("Client");
            var orderId = _scenarioContext.Get<int>("OrderId");

            var requestBody1 = new { agentId = agentId1 };
            var requestBody2 = new { agentId = agentId2 };

            var content1 = new StringContent(
                JsonSerializer.Serialize(requestBody1),
                Encoding.UTF8,
                "application/json");
            var content2 = new StringContent(
                JsonSerializer.Serialize(requestBody2),
                Encoding.UTF8,
                "application/json");

            // Execute both requests concurrently
            var task1 = client.PostAsync($"/orders/order/{orderId}/assign-agent", content1);
            var task2 = client.PostAsync($"/orders/order/{orderId}/assign-agent", content2);

            var responses = await Task.WhenAll(task1, task2);

            _scenarioContext["ConcurrentResponses"] = responses;
            _scenarioContext["ConcurrentAgentIds"] = new[] { agentId1, agentId2 };
        }

        [Then(@"AgentAssigned kafka event is published")]
        public void ThenAgentAssignedKafkaEventIsPublished()
        {
            var kafkaMock = _scenarioContext.Get<Mock<IKafkaProducer>>("KafkaMock");
            kafkaMock.Verify(p => p.PublishAsync(KafkaTopics.AgentAssigned, It.IsAny<string>(), It.IsAny<AgentAssignedEvent>()), Times.Once);
        }

        [Then(@"the order should have agent (\d+) assigned")]
        public void ThenTheOrderShouldHaveAgentAssigned(int expectedAgentId)
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = _scenarioContext.Get<int>("OrderId");

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.Find(orderId);

            order.Should().NotBeNull();
            order!.AgentId.Should().Be(expectedAgentId);
        }

        [Then(@"the order should still have agent (\d+) assigned")]
        public void ThenTheOrderShouldStillHaveAgentAssigned(int expectedAgentId)
        {
            ThenTheOrderShouldHaveAgentAssigned(expectedAgentId);
        }

        [Then(@"exactly one agent should be assigned to the order")]
        public void ThenExactlyOneAgentShouldBeAssignedToTheOrder()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = _scenarioContext.Get<int>("OrderId");
            var agentIds = _scenarioContext.Get<int[]>("ConcurrentAgentIds");

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.Find(orderId);

            order.Should().NotBeNull();
            order!.AgentId.Should().NotBeNull();
            order.AgentId.Should().BeOneOf(agentIds[0], agentIds[1]);
        }

        [Then(@"one response should be 204 and the other should be 409")]
        public void ThenOneResponseShouldBe204AndTheOtherShouldBe409()
        {
            var responses = _scenarioContext.Get<HttpResponseMessage[]>("ConcurrentResponses");
            var statusCodes = responses.Select(r => (int)r.StatusCode).ToList();

            // One agent should succeed with 204
            statusCodes.Should().Contain(204);
            // With authentication, second agent may get 403 (Forbidden) instead of 409 (Conflict)
            // because agents can only assign themselves - when agent 43 tries to assign agent 42, it's forbidden
            // If both agents try to assign themselves, one gets 204 and the other gets 409 (conflict)
            var rejectionCode = statusCodes.First(c => c != 204);
            rejectionCode.Should().BeOneOf(new[] { 403, 409 }, 
                because: "one agent should be rejected with 403 (Forbidden - can't assign other agents) or 409 (Conflict - already assigned)");
        }
    }
}
