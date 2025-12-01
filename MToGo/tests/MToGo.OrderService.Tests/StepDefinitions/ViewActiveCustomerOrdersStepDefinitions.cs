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
    public class ViewActiveCustomerOrdersStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private HttpResponseMessage? _response;
        private List<CustomerOrderResponse>? _orders;

        public ViewActiveCustomerOrdersStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        private HttpClient Client => _scenarioContext.Get<HttpClient>("Client");
        private SharedTestWebApplicationFactory Factory => _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");

        [Given(@"a customer with ID (\d+) has active and completed orders")]
        public async Task GivenACustomerWithIdHasActiveAndCompletedOrders(int customerId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 1,
                    DeliveryAddress = "Test Address 123",
                    DeliveryFee = 29,
                    ServiceFee = 6,
                    TotalAmount = 135,
                    Status = OrderStatus.Placed,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 50 }
                    }
                },
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 1,
                    DeliveryAddress = "Test Address 123",
                    DeliveryFee = 29,
                    ServiceFee = 6,
                    TotalAmount = 135,
                    Status = OrderStatus.Accepted,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 2, Name = "Burger", Quantity = 1, UnitPrice = 80 }
                    }
                },
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 2,
                    DeliveryAddress = "Test Address 456",
                    DeliveryFee = 35,
                    ServiceFee = 9,
                    TotalAmount = 194,
                    Status = OrderStatus.Delivered,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 3, Name = "Sushi", Quantity = 1, UnitPrice = 150 }
                    }
                }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"a customer with ID (\d+) has only completed orders")]
        public async Task GivenACustomerWithIdHasOnlyCompletedOrders(int customerId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 1,
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
                },
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 2,
                    DeliveryAddress = "Test Address 456",
                    DeliveryFee = 35,
                    ServiceFee = 9,
                    TotalAmount = 194,
                    Status = OrderStatus.Rejected,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 2, Name = "Burger", Quantity = 1, UnitPrice = 80 }
                    }
                }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"a customer with ID (\d+) has an order with status (\w+)")]
        public async Task GivenACustomerWithIdHasAnOrderWithStatus(int customerId, string status)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var orderStatus = Enum.Parse<OrderStatus>(status);

            var order = new Order
            {
                CustomerId = customerId,
                PartnerId = 1,
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

        [When(@"the customer requests their active orders")]
        public async Task WhenTheCustomerRequestsTheirActiveOrders()
        {
            _response = await Client.GetAsync("/orders/customer/1/active");
            if (_response.IsSuccessStatusCode)
            {
                _orders = await _response.Content.ReadFromJsonAsync<List<CustomerOrderResponse>>();
            }

            _scenarioContext["Response"] = _response;
            _scenarioContext["Orders"] = _orders;
        }

        [Then(@"the response contains only orders with active status for customer")]
        public void ThenTheResponseContainsOnlyOrdersWithActiveStatusForCustomer()
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
