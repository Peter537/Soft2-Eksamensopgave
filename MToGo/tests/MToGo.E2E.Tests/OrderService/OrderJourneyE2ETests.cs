extern alias OrderServiceApp;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using MToGo.OrderService.Models;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using CustomerModel = MToGo.CustomerService.Models.Customer;

namespace MToGo.E2E.Tests.OrderService;

public class OrderJourneyE2ETests : IClassFixture<OrderJourneyFixture>
{
    private readonly OrderJourneyFixture _fixture;
    private const int PartnerId = 501;
    private const int AgentId = 9401;

    public OrderJourneyE2ETests(OrderJourneyFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CustomerToDeliveryJourney_CompletesAcrossAllRoles()
    {
        await _fixture.ResetStateAsync();

        var uniqueEmail = $"hero.customer.{Guid.NewGuid():N}@example.com";
        var customerRequest = new CustomerModel
        {
            Name = "Hero Flow Customer",
            Email = uniqueEmail,
            DeliveryAddress = "Nørrebrogade 12, Copenhagen",
            NotificationMethod = "Email",
            Password = "Sup3rSecure!",
            PhoneNumber = "+4511122233",
            LanguagePreference = "en"
        };

        var customerId = await _fixture.RegisterCustomerAsync(customerRequest);
        _fixture.RunAsCustomer(customerId);

        var orderRequest = new OrderCreateRequest
        {
            CustomerId = customerId,
            PartnerId = PartnerId,
            DeliveryAddress = customerRequest.DeliveryAddress,
            DeliveryFee = 39,
            Distance = "4.2km",
            Items =
            {
                new OrderCreateItem { FoodItemId = 10, Name = "Truffle Burger", Quantity = 1, UnitPrice = 115 },
                new OrderCreateItem { FoodItemId = 11, Name = "Sweet Potato Fries", Quantity = 2, UnitPrice = 38 }
            }
        };

        var createResponse = await _fixture.OrderClient.PostAsJsonAsync("/orders/order", orderRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderCreateResponse>();
        Assert.NotNull(createdOrder);
        var orderId = createdOrder!.Id;

        _fixture.RunAsPartner(PartnerId);
        var acceptResponse = await _fixture.OrderClient.PostAsJsonAsync($"/orders/order/{orderId}/accept", new OrderAcceptRequest { EstimatedMinutes = 18 });
        Assert.Equal(HttpStatusCode.NoContent, acceptResponse.StatusCode);

        var readyResponse = await _fixture.OrderClient.PostAsync($"/orders/order/{orderId}/set-ready", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.NoContent, readyResponse.StatusCode);

        _fixture.RunAsAgent(AgentId);
        var assignResponse = await _fixture.OrderClient.PostAsJsonAsync($"/orders/order/{orderId}/assign-agent", new AssignAgentRequest { AgentId = AgentId });
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var pickupResponse = await _fixture.OrderClient.PostAsync($"/orders/order/{orderId}/pickup", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.NoContent, pickupResponse.StatusCode);

        var deliveryResponse = await _fixture.OrderClient.PostAsync($"/orders/order/{orderId}/complete-delivery", EmptyJsonContent());
        Assert.Equal(HttpStatusCode.NoContent, deliveryResponse.StatusCode);

        _fixture.RunAsCustomer(customerId);
        var detailResponse = await _fixture.OrderClient.GetAsync($"/orders/order/{orderId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<OrderDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal("Delivered", detail!.Status);
        Assert.Equal(AgentId, detail.AgentId);
        Assert.Equal(orderRequest.Items.Count, detail.Items.Count);

        var activeOrders = await _fixture.OrderClient.GetFromJsonAsync<List<CustomerOrderResponse>>($"/orders/customer/{customerId}/active");
        Assert.NotNull(activeOrders);
        Assert.Empty(activeOrders!);

        var orderHistory = await _fixture.OrderClient.GetFromJsonAsync<List<CustomerOrderResponse>>($"/orders/customer/{customerId}");
        Assert.NotNull(orderHistory);
        Assert.Single(orderHistory!);
        Assert.Equal("Delivered", orderHistory![0].Status);

        _fixture.RunAsPartner(PartnerId);
        var partnerOrders = await _fixture.OrderClient.GetFromJsonAsync<List<PartnerOrderResponse>>($"/orders/partner/{PartnerId}");
        Assert.NotNull(partnerOrders);
        Assert.Single(partnerOrders!);
        Assert.Equal("Delivered", partnerOrders![0].Status);

        _fixture.RunAsAgent(AgentId);
        var agentDeliveries = await _fixture.OrderClient.GetFromJsonAsync<List<AgentDeliveryResponse>>($"/orders/agent/{AgentId}");
        Assert.NotNull(agentDeliveries);
        Assert.Single(agentDeliveries!);
        Assert.Equal("Delivered", agentDeliveries![0].Status);

        Assert.Collection(_fixture.PublishedEvents,
            e => AssertKafkaEvent<OrderCreatedEvent>(KafkaTopics.OrderCreated, e),
            e => AssertKafkaEvent<OrderAcceptedEvent>(KafkaTopics.OrderAccepted, e),
            e => AssertKafkaEvent<OrderReadyEvent>(KafkaTopics.OrderReady, e),
            e => AssertKafkaEvent<AgentAssignedEvent>(KafkaTopics.AgentAssigned, e),
            e => AssertKafkaEvent<OrderPickedUpEvent>(KafkaTopics.OrderPickedUp, e),
            e => AssertKafkaEvent<OrderDeliveredEvent>(KafkaTopics.OrderDelivered, e));

        var deliveredEvent = (OrderDeliveredEvent)_fixture.PublishedEvents.Last().Payload;
        Assert.Equal(orderId, deliveredEvent.OrderId);
        Assert.Equal(customerId, deliveredEvent.CustomerId);
    }

    private static void AssertKafkaEvent<TEvent>(string topic, KafkaPublication publication)
    {
        Assert.Equal(topic, publication.Topic);
        Assert.IsType<TEvent>(publication.Payload);
    }

    private static StringContent EmptyJsonContent() => new("{}", Encoding.UTF8, "application/json");
}

