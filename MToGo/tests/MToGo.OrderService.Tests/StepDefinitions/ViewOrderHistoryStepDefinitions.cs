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
    public class ViewOrderHistoryStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private HttpResponseMessage? _response;
        private List<CustomerOrderResponse>? _orders;
        private DateTime _startDate;
        private DateTime _endDate;

        public ViewOrderHistoryStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        private HttpClient Client => _scenarioContext.Get<HttpClient>("Client");
        private SharedTestWebApplicationFactory Factory => _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");

        [Given(@"a customer with ID (\d+) has placed orders")]
        public async Task GivenACustomerWithIdHasPlacedOrders(int customerId)
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
                    Status = OrderStatus.Delivered,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 2, Name = "Burger", Quantity = 1, UnitPrice = 80 },
                        new OrderItem { FoodItemId = 3, Name = "Fries", Quantity = 2, UnitPrice = 35 }
                    }
                }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"a customer with ID (\d+) has no orders")]
        public void GivenACustomerWithIdHasNoOrders(int customerId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");
            // Database is cleaned before each scenario, so no orders exist
        }

        [Given(@"a customer with ID (\d+) has orders from different dates")]
        public async Task GivenACustomerWithIdHasOrdersFromDifferentDates(int customerId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // Order within the date range (will be included)
            _startDate = DateTime.UtcNow.AddDays(-7);
            _endDate = DateTime.UtcNow.AddDays(-1);

            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 1,
                    DeliveryAddress = "Test Address",
                    DeliveryFee = 29,
                    ServiceFee = 6,
                    TotalAmount = 135,
                    Status = OrderStatus.Delivered,
                    CreatedAt = DateTime.UtcNow.AddDays(-3), // Within range
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 1, UnitPrice = 100 }
                    }
                },
                new Order
                {
                    CustomerId = customerId,
                    PartnerId = 2,
                    DeliveryAddress = "Test Address",
                    DeliveryFee = 29,
                    ServiceFee = 6,
                    TotalAmount = 135,
                    Status = OrderStatus.Delivered,
                    CreatedAt = DateTime.UtcNow.AddDays(-30), // Outside range
                    Items = new List<OrderItem>
                    {
                        new OrderItem { FoodItemId = 2, Name = "Burger", Quantity = 1, UnitPrice = 100 }
                    }
                }
            };

            dbContext.Orders.AddRange(orders);
            await dbContext.SaveChangesAsync();
        }

        [Given(@"a customer with ID (\d+) has orders outside the date range")]
        public async Task GivenACustomerWithIdHasOrdersOutsideTheDateRange(int customerId)
        {
            TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // Date range that won't match any orders
            _startDate = DateTime.UtcNow.AddDays(-3);
            _endDate = DateTime.UtcNow.AddDays(-1);

            var order = new Order
            {
                CustomerId = customerId,
                PartnerId = 1,
                DeliveryAddress = "Test Address",
                DeliveryFee = 29,
                ServiceFee = 6,
                TotalAmount = 135,
                Status = OrderStatus.Delivered,
                CreatedAt = DateTime.UtcNow.AddDays(-30), // Outside range
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 1, UnitPrice = 100 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();
        }

        [When(@"the customer requests their order history")]
        public async Task WhenTheCustomerRequestsTheirOrderHistory()
        {
            _response = await Client.GetAsync("/orders/customer/1");
            if (_response.IsSuccessStatusCode)
            {
                _orders = await _response.Content.ReadFromJsonAsync<List<CustomerOrderResponse>>();
            }

            _scenarioContext["Response"] = _response;
        }

        [When(@"the customer requests order history with date range filter")]
        public async Task WhenTheCustomerRequestsOrderHistoryWithDateRangeFilter()
        {
            var startDateStr = _startDate.ToString("yyyy-MM-dd");
            var endDateStr = _endDate.ToString("yyyy-MM-dd");
            
            _response = await Client.GetAsync($"/orders/customer/1?startDate={startDateStr}&endDate={endDateStr}");
            if (_response.IsSuccessStatusCode)
            {
                _orders = await _response.Content.ReadFromJsonAsync<List<CustomerOrderResponse>>();
            }

            _scenarioContext["Response"] = _response;
        }

        [Then(@"the response contains a list of orders with dates, partners, items, and totals")]
        public void ThenTheResponseContainsAListOfOrdersWithDatesPartnersItemsAndTotals()
        {
            _orders.Should().NotBeNull();
            _orders.Should().HaveCountGreaterThan(0);

            foreach (var order in _orders!)
            {
                order.Id.Should().BeGreaterThan(0);
                order.PartnerId.Should().BeGreaterThan(0);
                order.OrderCreatedTime.Should().NotBeNullOrEmpty();
                order.Items.Should().NotBeEmpty();
                
                // Verify totals are present
                order.ServiceFee.Should().BeGreaterThanOrEqualTo(0);
                order.DeliveryFee.Should().BeGreaterThanOrEqualTo(0);
            }
        }

        [Then(@"the response contains an empty list")]
        public void ThenTheResponseContainsAnEmptyList()
        {
            // Try to get orders from ScenarioContext first (for shared usage), fall back to local field
            var orders = _scenarioContext.TryGetValue("Orders", out var contextOrders) 
                ? contextOrders as List<CustomerOrderResponse> 
                : _orders;
            orders.Should().NotBeNull();
            orders.Should().BeEmpty();
        }

        [Then(@"only orders within the date range are returned")]
        public void ThenOnlyOrdersWithinTheDateRangeAreReturned()
        {
            _orders.Should().NotBeNull();
            _orders.Should().HaveCount(1); // Only one order within range

            foreach (var order in _orders!)
            {
                var createdAt = DateTime.Parse(order.OrderCreatedTime);
                createdAt.Should().BeOnOrAfter(_startDate.Date);
                createdAt.Should().BeOnOrBefore(_endDate.Date.AddDays(1)); // Include the entire end day
            }
        }
    }
}
