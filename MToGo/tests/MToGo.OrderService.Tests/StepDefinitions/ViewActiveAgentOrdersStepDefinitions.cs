using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Tests.Fixtures;
using MToGo.Testing;
using Reqnroll;
using System.Net.Http.Json;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class ViewActiveAgentOrdersStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private HttpResponseMessage? _response;
        private List<CustomerOrderResponse>? _orders;

        public ViewActiveAgentOrdersStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        private HttpClient Client => _scenarioContext.Get<HttpClient>("Client");
        private SharedTestWebApplicationFactory Factory => _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");

        [Given(@"an agent with ID (\d+) has active and completed orders")]
        public async Task GivenAnAgentWithIdHasActiveAndCompletedOrders(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = 100,
                    PartnerId = 10,
                    AgentId = agentId,
                    DeliveryAddress = "Test Address 123",
                    DeliveryFee = 29,
                    ServiceFee = 6,
                    TotalAmount = 135,
                    Status = OrderStatus.PickedUp,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 50 }
                    }
                },
                new Order
                {
                    CustomerId = 101,
                    PartnerId = 10,
                    AgentId = agentId,
                    DeliveryAddress = "Test Address 456",
                    DeliveryFee = 35,
                    ServiceFee = 9,
                    TotalAmount = 194,
                    Status = OrderStatus.Delivered,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 2, Name = "Burger", Quantity = 1, UnitPrice = 80 }
                    }
                }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"an agent with ID (\d+) has only completed orders")]
        public async Task GivenAnAgentWithIdHasOnlyCompletedOrders(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = new Order
            {
                CustomerId = 100,
                PartnerId = 10,
                AgentId = agentId,
                DeliveryAddress = "Test Address 123",
                DeliveryFee = 29,
                ServiceFee = 6,
                TotalAmount = 135,
                Status = OrderStatus.Delivered,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 50 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"an agent with ID (\d+) has no orders")]
        public void GivenAnAgentWithIdHasNoOrders(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
            // Database is cleaned before each scenario, so no orders exist
        }

        [Given(@"an agent with ID (\d+) has an order with status (\w+)")]
        public async Task GivenAnAgentWithIdHasAnOrderWithStatus(int agentId, string status)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var orderStatus = Enum.Parse<OrderStatus>(status);

            var order = new Order
            {
                CustomerId = 100,
                PartnerId = 10,
                AgentId = agentId,
                DeliveryAddress = "Test Address 123",
                DeliveryFee = 29,
                ServiceFee = 6,
                TotalAmount = 135,
                Status = orderStatus,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 50 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
        }

        [When(@"the agent requests their active orders")]
        public async Task WhenTheAgentRequestsTheirActiveOrders()
        {
            _response = await Client.GetAsync("/orders/agent/1/active");
            if (_response.IsSuccessStatusCode)
            {
                _orders = await _response.Content.ReadFromJsonAsync<List<CustomerOrderResponse>>();
            }

            _scenarioContext["Response"] = _response;
            _scenarioContext["Orders"] = _orders;
        }

        [Then(@"the response contains only orders with active status for agent")]
        public void ThenTheResponseContainsOnlyOrdersWithActiveStatusForAgent()
        {
            _orders.Should().NotBeNull();
            _orders.Should().HaveCountGreaterThan(0);

            foreach (var order in _orders!)
            {
                order.Status.Should().NotBe("Delivered");
                order.Status.Should().NotBe("Rejected");
            }
        }
    }
}

