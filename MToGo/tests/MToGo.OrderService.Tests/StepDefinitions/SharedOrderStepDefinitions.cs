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

        [Given(@"an order exists with Accepted status")]
        public async Task GivenAnOrderExistsWithAcceptedStatus()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Accepted);
            _scenarioContext["OrderId"] = orderId;
        }

        [Given(@"an order exists with Rejected status")]
        public async Task GivenAnOrderExistsWithRejectedStatus()
        {
            var factory = _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");
            var orderId = await OrderTestHelper.CreateOrderWithStatus(factory, OrderStatus.Rejected);
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
