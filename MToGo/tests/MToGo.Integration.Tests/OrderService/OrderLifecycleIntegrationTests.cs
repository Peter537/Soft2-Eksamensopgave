using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MToGo.Integration.Tests.OrderService.Fixtures;
using MToGo.OrderService.Models;
using MToGo.Testing;

namespace MToGo.Integration.Tests.OrderService;

[Collection("OrderService Integration Tests")]
public class OrderLifecycleIntegrationTests
{
    private readonly OrderServiceIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public OrderLifecycleIntegrationTests(OrderServiceIntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CustomerOrder_FullLifecycle_CompletesSuccessfully()
    {
        await _fixture.ResetStateAsync();

        const int customerId = 1;
        const int partnerId = 42;
        const int agentId = 7;

        var orderId = await CreateOrderAsync(customerId, partnerId, 149.00m);

        await AcceptOrderAsync(orderId, partnerId);
        await SetReadyAsync(orderId, partnerId);
        await AssignAgentAsync(orderId, agentId);
        await PickupAsync(orderId, agentId);
        await CompleteDeliveryAsync(orderId, agentId);

        TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");
        var detailResponse = await _client.GetAsync($"/orders/order/{orderId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<OrderDetailResponse>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(orderId);
        detail.Status.Should().Be("Delivered");
        detail.AgentId.Should().Be(agentId);
    }

    [Fact]
    public async Task AssignAgent_WithMismatchedUserId_ReturnsForbid()
    {
        await _fixture.ResetStateAsync();

        var orderId = await CreateOrderAsync(customerId: 5, partnerId: 9, totalValue: 99m);

        TestAuthenticationHandler.SetTestUser("9", "Partner");
        await _client.PostAsJsonAsync($"/orders/order/{orderId}/accept", new OrderAcceptRequest { EstimatedMinutes = 10 });

        TestAuthenticationHandler.SetTestUser("100", "Agent");
        var assignResponse = await _client.PostAsJsonAsync($"/orders/order/{orderId}/assign-agent", new AssignAgentRequest
        {
            AgentId = 99
        });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<int> CreateOrderAsync(int customerId, int partnerId, decimal totalValue)
    {
        TestAuthenticationHandler.SetTestUser(customerId.ToString(), "Customer");

        var request = new OrderCreateRequest
        {
            CustomerId = customerId,
            PartnerId = partnerId,
            DeliveryAddress = "Test Street 1",
            DeliveryFee = 15,
            Distance = "3km",
            Items = new List<OrderCreateItem>
            {
                new() { FoodItemId = 1, Name = "Burger", Quantity = 1, UnitPrice = totalValue }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders/order", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OrderCreateResponse>();
        payload.Should().NotBeNull();
        return payload!.Id;
    }

    private async Task AcceptOrderAsync(int orderId, int partnerId)
    {
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");
        var response = await _client.PostAsJsonAsync($"/orders/order/{orderId}/accept", new OrderAcceptRequest
        {
            EstimatedMinutes = 20
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task SetReadyAsync(int orderId, int partnerId)
    {
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");
        var response = await _client.PostAsync($"/orders/order/{orderId}/set-ready", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task AssignAgentAsync(int orderId, int agentId)
    {
        TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        var response = await _client.PostAsJsonAsync($"/orders/order/{orderId}/assign-agent", new AssignAgentRequest
        {
            AgentId = agentId
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task PickupAsync(int orderId, int agentId)
    {
        TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        var response = await _client.PostAsync($"/orders/order/{orderId}/pickup", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task CompleteDeliveryAsync(int orderId, int agentId)
    {
        TestAuthenticationHandler.SetTestUser(agentId.ToString(), "Agent");
        var response = await _client.PostAsync($"/orders/order/{orderId}/complete-delivery", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

