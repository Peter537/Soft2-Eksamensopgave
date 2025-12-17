using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MToGo.Integration.Tests.OrderService.Fixtures;
using MToGo.OrderService.Models;
using MToGo.Testing;

namespace MToGo.Integration.Tests.OrderService;

[Collection("OrderService Integration Tests")]
public class OrderVisibilityIntegrationTests
{
    private readonly OrderServiceIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public OrderVisibilityIntegrationTests(OrderServiceIntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetCustomerOrders_ReturnsOnlyOrdersBelongingToCustomer()
    {
        await _fixture.ResetStateAsync();

        var orderOne = await CreateOrderAsync(customerId: 1, partnerId: 1, totalValue: 120m);
        var orderTwo = await CreateOrderAsync(customerId: 1, partnerId: 2, totalValue: 80m);
        await CreateOrderAsync(customerId: 2, partnerId: 3, totalValue: 55m);

        TestAuthenticationHandler.SetTestUser("1", "Customer");
        var response = await _client.GetAsync("/orders/customer/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<List<CustomerOrderResponse>>();
        orders.Should().NotBeNull();
        orders!.Select(o => o.Id).Should().BeEquivalentTo(new[] { orderOne, orderTwo });
    }

    [Fact]
    public async Task GetOrderDetail_ForDifferentCustomer_ReturnsForbidden()
    {
        await _fixture.ResetStateAsync();

        var orderId = await CreateOrderAsync(customerId: 5, partnerId: 1, totalValue: 70m);

        TestAuthenticationHandler.SetTestUser("6", "Customer");
        var response = await _client.GetAsync($"/orders/order/{orderId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCustomerOrders_CanFilterActiveOrders()
    {
        await _fixture.ResetStateAsync();

        var activeOrderId = await CreateOrderAsync(customerId: 3, partnerId: 10, totalValue: 110m);
        var deliveredOrderId = await CreateOrderAsync(customerId: 3, partnerId: 11, totalValue: 90m);

        await ProgressOrderToDeliveredAsync(deliveredOrderId, partnerId: 11, agentId: 33);

        TestAuthenticationHandler.SetTestUser("3", "Customer");
        var activeResponse = await _client.GetAsync("/orders/customer/3/active");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeOrders = await activeResponse.Content.ReadFromJsonAsync<List<CustomerOrderResponse>>();
        activeOrders.Should().NotBeNull();
        activeOrders!.Should().ContainSingle(o => o.Id == activeOrderId);
        activeOrders.Should().NotContain(o => o.Id == deliveredOrderId);
    }

    private async Task<int> CreateOrderAsync(int customerId, int partnerId, decimal totalValue)
    {
        TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

        var request = new OrderCreateRequest
        {
            CustomerId = customerId,
            PartnerId = partnerId,
            DeliveryAddress = "Visibility Street",
            DeliveryFee = 10,
            Distance = "2km",
            Items = new List<OrderCreateItem>
            {
                new() { FoodItemId = 10, Name = "Item", Quantity = 1, UnitPrice = totalValue }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders/order", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OrderCreateResponse>();
        payload.Should().NotBeNull();
        return payload!.Id;
    }

    private async Task ProgressOrderToDeliveredAsync(int orderId, int partnerId, int agentId)
    {
        await AcceptAsync(orderId, partnerId);
        await SetReadyAsync(orderId, partnerId);
        await AssignAsync(orderId, agentId);
        await PickupAsync(orderId, agentId);
        await CompleteAsync(orderId, agentId);
    }

    private async Task AcceptAsync(int orderId, int partnerId)
    {
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");
        await _client.PostAsJsonAsync($"/orders/order/{orderId}/accept", new OrderAcceptRequest { EstimatedMinutes = 12 });
    }

    private async Task SetReadyAsync(int orderId, int partnerId)
    {
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");
        await _client.PostAsync($"/orders/order/{orderId}/set-ready", null);
    }

    private async Task AssignAsync(int orderId, int agentId)
    {
        TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        await _client.PostAsJsonAsync($"/orders/order/{orderId}/assign-agent", new AssignAgentRequest { AgentId = agentId });
    }

    private async Task PickupAsync(int orderId, int agentId)
    {
        TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        await _client.PostAsync($"/orders/order/{orderId}/pickup", null);
    }

    private async Task CompleteAsync(int orderId, int agentId)
    {
        TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        await _client.PostAsync($"/orders/order/{orderId}/complete-delivery", null);
    }
}

