using Reqnroll;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MToGo.OrderService.Models;
using MToGo.OrderService.Entities;
using Moq;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using FluentAssertions;
using static Moq.Times;

namespace MToGo.OrderService.Tests.StepDefinitions
{
    [Binding]
    public class PlaceFoodOrderStepDefinitions
    {
        private TestWebApplicationFactory? _factory;
        private HttpClient? _client;
        private OrderCreateRequest _request = new();
        private HttpResponseMessage? _response;
        private OrderCreateResponse? _responseData;
        private Mock<IKafkaProducer> _kafkaMock = new();

        [BeforeScenario]
        public async Task Setup()
        {
            _kafkaMock = new Mock<IKafkaProducer>();
            _factory = new TestWebApplicationFactory(_kafkaMock);
            await _factory.InitializeAsync();
            _client = _factory.CreateClient();
            _request = new OrderCreateRequest();
        }

        [AfterScenario]
        public async Task TearDown()
        {
            if (_factory != null)
            {
                await _factory.DisposeAsync();
            }
        }

        [Given(@"items in cart")]
        public void GivenItemsInCart()
        {
            _request.Items = new List<OrderCreateItem>
            {
                new OrderCreateItem { FoodItemId = 1, Name = "Pizza", Quantity = 1, UnitPrice = 100 }
            };
        }

        [Given(@"valid payment")]
        public void GivenValidPayment()
        {
            // Forventet at betalingen er gyldig
        }

        [When(@"submitting the order")]
        public async Task WhenSubmittingTheOrder()
        {
            _response = await _client!.PostAsJsonAsync("/orders/order", _request);
            if (_response.IsSuccessStatusCode)
            {
                _responseData = await _response.Content.ReadFromJsonAsync<OrderCreateResponse>();
            }
        }

        [Then(@"order is created in Placed status with an ID")]
        public void ThenOrderIsCreatedInPlacedStatusWithAnID()
        {
            _response!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            _responseData.Should().NotBeNull();
            _responseData!.Id.Should().BeGreaterThan(0);
        }

        [Given(@"successful order creation")]
        public void GivenSuccessfulOrderCreation()
        {
            // Forventet at ordren oprettes succesfuldt
        }

        [When(@"order is created")]
        public async Task WhenOrderIsCreated()
        {
            await WhenSubmittingTheOrder();
        }

        [Then(@"OrderCreated kafka event is published")]
        public void ThenOrderCreatedKafkaEventIsPublished()
        {
            _kafkaMock.Verify(p => p.PublishAsync(KafkaTopics.OrderCreated, It.IsAny<string>(), It.IsAny<OrderCreatedEvent>()), Times.Once);
        }

        [Given(@"an order with total value of ([\d.]+) DKK")]
        public void GivenAnOrderWithTotalValueOf(decimal value)
        {
            _request.Items = new List<OrderCreateItem>
            {
                new OrderCreateItem { FoodItemId = 1, Name = "Item", Quantity = 1, UnitPrice = value }
            };
        }

        [When(@"the order is placed")]
        public async Task WhenTheOrderIsPlaced()
        {
            await WhenSubmittingTheOrder();
        }

        [Then(@"the service fee should be ([\d.]+) DKK")]
        public void ThenTheServiceFeeShouldBe(decimal expectedFee)
        {
            _response!.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            _responseData.Should().NotBeNull();

            // Verifier ServiceFee i databasen
            using var scope = _factory!.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.Find(_responseData!.Id);
            
            order.Should().NotBeNull();
            order!.ServiceFee.Should().Be(expectedFee);
        }
    }
}