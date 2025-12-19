using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Tests.Fixtures;
using MToGo.Testing;
using Reqnroll;
using System.Net;
using System.Net.Http.Json;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class ViewOrderDetailsStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private HttpResponseMessage? _response;
        private OrderDetailResponse? _orderDetail;

        public ViewOrderDetailsStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        private HttpClient Client => _scenarioContext.Get<HttpClient>("Client");
        private SharedTestWebApplicationFactory Factory => _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");

        [Given(@"a customer with ID (\d+) has an order with ID (\d+)")]
        public async Task GivenACustomerWithIdHasAnOrderWithId(int customerId, int orderId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = new Order
            {
                CustomerId = customerId,
                PartnerId = 10,
                AgentId = null,
                DeliveryAddress = "Test Address 123",
                DeliveryFee = 29,
                ServiceFee = 6,
                TotalAmount = 135,
                Status = OrderStatus.Delivered,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 1, Name = "Pizza Margherita", Quantity = 2, UnitPrice = 50 },
                    new OrderItem { FoodItemId = 2, Name = "Garlic Bread", Quantity = 1, UnitPrice = 35 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"a partner with ID (\d+) has an order with ID (\d+)")]
        public async Task GivenAPartnerWithIdHasAnOrderWithId(int partnerId, int orderId)
        {
            TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = new Order
            {
                CustomerId = 100,
                PartnerId = partnerId,
                AgentId = null,
                DeliveryAddress = "Customer Address 456",
                DeliveryFee = 35,
                ServiceFee = 9,
                TotalAmount = 194,
                Status = OrderStatus.Accepted,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 3, Name = "Burger Deluxe", Quantity = 1, UnitPrice = 80 },
                    new OrderItem { FoodItemId = 4, Name = "Fries", Quantity = 2, UnitPrice = 35 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"an agent with ID (\d+) is assigned to order with ID (\d+)")]
        public async Task GivenAnAgentWithIdIsAssignedToOrderWithId(int agentId, int orderId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = new Order
            {
                CustomerId = 100,
                PartnerId = 10,
                AgentId = agentId,
                DeliveryAddress = "Delivery Address 789",
                DeliveryFee = 40,
                ServiceFee = 12,
                TotalAmount = 252,
                Status = OrderStatus.PickedUp,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 5, Name = "Sushi Platter", Quantity = 1, UnitPrice = 200 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"a customer with ID (\d+) is authenticated")]
        public void GivenACustomerWithIdIsAuthenticated(int customerId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");
        }

        [Given(@"a partner with ID (\d+) is authenticated")]
        public void GivenAPartnerWithIdIsAuthenticated(int partnerId)
        {
            TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");
        }

        [Given(@"an agent with ID (\d+) is authenticated")]
        public void GivenAnAgentWithIdIsAuthenticated(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        }

        [When(@"the customer requests order details for order ID (\d+)")]
        public async Task WhenTheCustomerRequestsOrderDetailsForOrderId(int orderId)
        {
            _response = await Client.GetAsync($"/orders/order/{orderId}");
            if (_response.IsSuccessStatusCode)
            {
                _orderDetail = await _response.Content.ReadFromJsonAsync<OrderDetailResponse>();
            }

            _scenarioContext["Response"] = _response;
        }

        [When(@"the partner requests order details for order ID (\d+)")]
        public async Task WhenThePartnerRequestsOrderDetailsForOrderId(int orderId)
        {
            _response = await Client.GetAsync($"/orders/order/{orderId}");
            if (_response.IsSuccessStatusCode)
            {
                _orderDetail = await _response.Content.ReadFromJsonAsync<OrderDetailResponse>();
            }

            _scenarioContext["Response"] = _response;
        }

        [When(@"the agent requests order details for order ID (\d+)")]
        public async Task WhenTheAgentRequestsOrderDetailsForOrderId(int orderId)
        {
            _response = await Client.GetAsync($"/orders/order/{orderId}");
            if (_response.IsSuccessStatusCode)
            {
                _orderDetail = await _response.Content.ReadFromJsonAsync<OrderDetailResponse>();
            }

            _scenarioContext["Response"] = _response;
        }

        [Then(@"the response contains order details including items, status, dates, and totals")]
        public void ThenTheResponseContainsOrderDetailsIncludingItemsStatusDatesAndTotals()
        {
            _orderDetail.Should().NotBeNull();
            _orderDetail!.Id.Should().BeGreaterThan(0);
            _orderDetail.CustomerId.Should().BeGreaterThan(0);
            _orderDetail.PartnerId.Should().BeGreaterThan(0);
            _orderDetail.Status.Should().NotBeNullOrEmpty();
            _orderDetail.OrderCreatedTime.Should().NotBeNullOrEmpty();
            _orderDetail.DeliveryAddress.Should().NotBeNullOrEmpty();
            _orderDetail.ServiceFee.Should().BeGreaterThanOrEqualTo(0);
            _orderDetail.DeliveryFee.Should().BeGreaterThanOrEqualTo(0);
            _orderDetail.Items.Should().NotBeEmpty();

            foreach (var item in _orderDetail.Items)
            {
                item.FoodItemId.Should().BeGreaterThan(0);
                item.Name.Should().NotBeNullOrEmpty();
                item.Quantity.Should().BeGreaterThan(0);
                item.UnitPrice.Should().BeGreaterThan(0);
            }
        }
    }
}

