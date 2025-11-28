using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Entities;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using FluentAssertions;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class AcceptOrderStepDefinitions
    {
        private TestWebApplicationFactory? _factory;
        private HttpClient? _client;
        private HttpResponseMessage? _response;
        private Mock<IKafkaProducer> _kafkaMock = new();
        private int _orderId;

        [BeforeScenario]
        public async Task Setup()
        {
            _kafkaMock = new Mock<IKafkaProducer>();
            _factory = new TestWebApplicationFactory(_kafkaMock);
            await _factory.InitializeAsync();
            _client = _factory.CreateClient();
        }

        [AfterScenario]
        public async Task TearDown()
        {
            if (_factory != null)
            {
                await _factory.DisposeAsync();
            }
        }

        [Given(@"an order exists with Placed status")]
        public async Task GivenAnOrderExistsWithPlacedStatus()
        {
            _orderId = await CreateOrderWithStatus(OrderStatus.Placed);
        }

        [Given(@"an order exists with Accepted status")]
        public async Task GivenAnOrderExistsWithAcceptedStatus()
        {
            _orderId = await CreateOrderWithStatus(OrderStatus.Accepted);
        }

        [When(@"the partner accepts the order")]
        public async Task WhenThePartnerAcceptsTheOrder()
        {
            _response = await _client!.PostAsync($"/orders/order/{_orderId}/accept", null);
        }

        [Then(@"the order status should be Accepted")]
        public void ThenTheOrderStatusShouldBeAccepted()
        {
            using var scope = _factory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.Find(_orderId);

            order.Should().NotBeNull();
            order!.Status.Should().Be(OrderStatus.Accepted);
        }

        [Then(@"the response status code should be (\d+)")]
        public void ThenTheResponseStatusCodeShouldBe(int statusCode)
        {
            ((int)_response!.StatusCode).Should().Be(statusCode);
        }

        [Then(@"OrderAccepted kafka event is published")]
        public void ThenOrderAcceptedKafkaEventIsPublished()
        {
            _kafkaMock.Verify(p => p.PublishAsync(KafkaTopics.OrderAccepted, It.IsAny<string>(), It.IsAny<OrderAcceptedEvent>()), Times.Once);
        }

        private async Task<int> CreateOrderWithStatus(OrderStatus status)
        {
            using var scope = _factory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = new Order
            {
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test Address 123",
                DeliveryFee = 29,
                ServiceFee = 6,
                TotalAmount = 135,
                Status = status,
                Items = new List<OrderItem>
                {
                    new OrderItem { FoodItemId = 1, Name = "Pizza", Quantity = 1, UnitPrice = 100 }
                }
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            return order.Id;
        }
    }
}
