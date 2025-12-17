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
    public class ViewAgentDeliveryHistoryStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private HttpResponseMessage? _response;
        private List<AgentDeliveryResponse>? _deliveries;
        private DateTime _startDate;
        private DateTime _endDate;

        public ViewAgentDeliveryHistoryStepDefinitions(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        private HttpClient Client => _scenarioContext.Get<HttpClient>("Client");
        private SharedTestWebApplicationFactory Factory => _scenarioContext.Get<SharedTestWebApplicationFactory>("Factory");

        [Given(@"an agent with ID (\d+) has completed deliveries")]
        public async Task GivenAnAgentWithIdHasCompletedDeliveries(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = 10,
                    PartnerId = 1,
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
                },
                new Order
                {
                    CustomerId = 20,
                    PartnerId = 2,
                    AgentId = agentId,
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

        [Given(@"an agent with ID (\d+) has no deliveries")]
        public void GivenAnAgentWithIdHasNoDeliveries(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
            // Database is cleaned before each scenario, so no orders exist
        }

        [Given(@"an agent with ID (\d+) has deliveries from different dates")]
        public async Task GivenAnAgentWithIdHasDeliveriesFromDifferentDates(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // Delivery within the date range (will be included)
            _startDate = DateTime.UtcNow.AddDays(-7);
            _endDate = DateTime.UtcNow.AddDays(-1);

            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = 10,
                    PartnerId = 1,
                    AgentId = agentId,
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
                    CustomerId = 20,
                    PartnerId = 2,
                    AgentId = agentId,
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

        [Given(@"an agent with ID (\d+) has deliveries outside the date range")]
        public async Task GivenAnAgentWithIdHasDeliveriesOutsideTheDateRange(int agentId)
        {
            TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // Date range that won't match any orders
            _startDate = DateTime.UtcNow.AddDays(-3);
            _endDate = DateTime.UtcNow.AddDays(-1);

            var order = new Order
            {
                CustomerId = 10,
                PartnerId = 1,
                AgentId = agentId,
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

        [When(@"the agent requests their delivery history")]
        public async Task WhenTheAgentRequestsTheirDeliveryHistory()
        {
            _response = await Client.GetAsync("/orders/agent/1");
            if (_response.IsSuccessStatusCode)
            {
                _deliveries = await _response.Content.ReadFromJsonAsync<List<AgentDeliveryResponse>>();
            }

            _scenarioContext["Response"] = _response;
        }

        [When(@"the agent requests delivery history with date range filter")]
        public async Task WhenTheAgentRequestsDeliveryHistoryWithDateRangeFilter()
        {
            var startDateStr = _startDate.ToString("yyyy-MM-dd");
            var endDateStr = _endDate.ToString("yyyy-MM-dd");
            
            _response = await Client.GetAsync($"/orders/agent/1?startDate={startDateStr}&endDate={endDateStr}");
            if (_response.IsSuccessStatusCode)
            {
                _deliveries = await _response.Content.ReadFromJsonAsync<List<AgentDeliveryResponse>>();
            }

            _scenarioContext["Response"] = _response;
        }

        [Then(@"the response contains a list of deliveries with dates, partners, customers, and delivery fees")]
        public void ThenTheResponseContainsAListOfDeliveriesWithDatesPartnersCustomersAndDeliveryFees()
        {
            _deliveries.Should().NotBeNull();
            _deliveries.Should().HaveCountGreaterThan(0);

            foreach (var delivery in _deliveries!)
            {
                delivery.Id.Should().BeGreaterThan(0);
                delivery.PartnerId.Should().BeGreaterThan(0);
                delivery.CustomerId.Should().BeGreaterThan(0);
                delivery.OrderCreatedTime.Should().NotBeNullOrEmpty();
                delivery.DeliveryFee.Should().BeGreaterThanOrEqualTo(0);
                delivery.Items.Should().NotBeEmpty();
            }
        }

        [Then(@"only deliveries within the date range are returned")]
        public void ThenOnlyDeliveriesWithinTheDateRangeAreReturned()
        {
            _deliveries.Should().NotBeNull();
            _deliveries.Should().HaveCount(1); // Only one delivery within range

            foreach (var delivery in _deliveries!)
            {
                var createdAt = DateTime.Parse(delivery.OrderCreatedTime);
                createdAt.Should().BeOnOrAfter(_startDate.Date);
                createdAt.Should().BeOnOrBefore(_endDate.Date.AddDays(1)); // Include the entire end day
            }
        }

        [Then(@"the delivery response contains an empty list")]
        public void ThenTheDeliveryResponseContainsAnEmptyList()
        {
            _deliveries.Should().NotBeNull();
            _deliveries.Should().BeEmpty();
        }
    }
}

