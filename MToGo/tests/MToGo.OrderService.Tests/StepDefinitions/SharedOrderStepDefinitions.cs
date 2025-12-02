using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Tests.Fixtures;
using MToGo.OrderService.Tests.Helpers;
using FluentAssertions;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class SharedOrderStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;

        public SharedOrderStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [Given(@"an order exists with Placed status")]
        public async Task GivenAnOrderExistsWithPlacedStatus()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Placed);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Placed status and no agent assigned")]
        public async Task GivenAnOrderExistsWithPlacedStatusAndNoAgentAssigned()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Placed, agentId: null);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Accepted status")]
        public async Task GivenAnOrderExistsWithAcceptedStatus()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Accepted);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Accepted status and no agent assigned")]
        public async Task GivenAnOrderExistsWithAcceptedStatusAndNoAgentAssigned()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Accepted, agentId: null);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Rejected status")]
        public async Task GivenAnOrderExistsWithRejectedStatus()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Rejected);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Ready status and no agent assigned")]
        public async Task GivenAnOrderExistsWithReadyStatusAndNoAgentAssigned()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Ready, agentId: null);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Ready status and agent (\d+) assigned")]
        public async Task GivenAnOrderExistsWithReadyStatusAndAgentAssigned(int agentId)
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Ready, agentId: agentId);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with PickedUp status and no agent assigned")]
        public async Task GivenAnOrderExistsWithPickedUpStatusAndNoAgentAssigned()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.PickedUp, agentId: null);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with PickedUp status and agent (\d+) assigned")]
        public async Task GivenAnOrderExistsWithPickedUpStatusAndAgentAssigned(int agentId)
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.PickedUp, agentId: agentId);
            _scenarioContext["OrderId"] = orderId;
        }

        [Then(@"the response status code should be (\d+)")]
        public void ThenTheResponseStatusCodeShouldBe(int statusCode)
        {
            var response = _scenarioContext.Get<HttpResponseMessage>("Response");
            ((int)response.StatusCode).Should().Be(statusCode);
        }

        [Then(@"the order status should be Accepted")]
        public void ThenTheOrderStatusShouldBeAccepted()
        {
            VerifyOrderStatus(OrderStatus.Accepted);
        }

        [Then(@"the order status should be Rejected")]
        public void ThenTheOrderStatusShouldBeRejected()
        {
            VerifyOrderStatus(OrderStatus.Rejected);
        }

        [Then(@"the order status should be Ready")]
        public void ThenTheOrderStatusShouldBeReady()
        {
            VerifyOrderStatus(OrderStatus.Ready);
        }

        [Then(@"the order status should be PickedUp")]
        public void ThenTheOrderStatusShouldBePickedUp()
        {
            VerifyOrderStatus(OrderStatus.PickedUp);
        }

        [Then(@"the order status should be Delivered")]
        public void ThenTheOrderStatusShouldBeDelivered()
        {
            VerifyOrderStatus(OrderStatus.Delivered);
        }

        private void VerifyOrderStatus(OrderStatus expectedStatus)
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = _scenarioContext.Get<int>("OrderId");

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.Find(orderId);

            order.Should().NotBeNull();
            order!.Status.Should().Be(expectedStatus);
        }
    }
}
