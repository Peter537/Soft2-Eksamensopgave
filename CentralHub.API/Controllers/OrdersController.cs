using Microsoft.AspNetCore.Mvc;
using CentralHub.API.Models;

namespace CentralHub.API.Controllers;

/// <summary>
/// API Gateway Controller - Routes requests to backend services.
/// NO BUSINESS LOGIC HERE - just coordination and routing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrdersController> _logger;
    private readonly string _orderServiceUrl;
    private readonly string _partnerServiceUrl;

    public OrdersController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OrdersController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _orderServiceUrl = configuration["ServiceUrls:OrderService"] ?? "http://localhost:5100";
        _partnerServiceUrl = configuration["ServiceUrls:PartnerService"] ?? "http://localhost:5220";
    }

    /// <summary>
    /// Gateway endpoint: Creates an order by routing to OrderService.
    /// OrderService handles: DB save + Kafka event publishing.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  � GATEWAY: Routing order creation to OrderService     │");
            Console.WriteLine("└─────────────────────────────────────────────────────────┘");
            Console.WriteLine($"   Customer: {request.CustomerName}");
            Console.WriteLine($"   Routing to: {_orderServiceUrl}/api/orders");

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{_orderServiceUrl}/api/orders", request);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<Order>();
                Console.WriteLine($"   ✅ Gateway: Order routed successfully\n");
                return CreatedAtAction(nameof(GetOrder), new { id = order?.OrderId }, order);
            }
            else
            {
                Console.WriteLine($"   ❌ Gateway: OrderService returned {response.StatusCode}\n");
                return StatusCode((int)response.StatusCode, "Failed to create order");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ GATEWAY ERROR: {ex.Message}\n");
            _logger.LogError(ex, "❌ Failed to route order creation");
            return StatusCode(500, "Gateway error: Failed to route request");
        }
    }

    /// <summary>
    /// Gateway endpoint: Get order by ID.
    /// Checks PartnerService first (has full order lifecycle), then OrderService.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            
            // Try PartnerService first (has order state)
            var response = await client.GetAsync($"{_partnerServiceUrl}/api/orders/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<Order>();
                return Ok(order);
            }
            
            // Fallback to OrderService
            response = await client.GetAsync($"{_orderServiceUrl}/api/orders/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<Order>();
                return Ok(order);
            }

            return NotFound($"Order {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order {OrderId}", id);
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Get all orders from PartnerService.
    /// PartnerService manages order state after creation.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_partnerServiceUrl}/api/orders");

            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return Ok(orders);
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all orders");
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Accept order (routes to PartnerService).
    /// Business logic handled by PartnerService.
    /// </summary>
    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptOrder(string id)
    {
        try
        {
            Console.WriteLine($"\n🔀 Gateway: Routing accept request to PartnerService for order {id}");
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"{_partnerServiceUrl}/api/orders/{id}/accept", null);

            Console.WriteLine($"   ✅ Gateway: Accept request routed\n");
            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route accept for order {OrderId}", id);
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Reject order (routes to PartnerService).
    /// Business logic handled by PartnerService.
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectOrder(string id, [FromBody] string reason)
    {
        try
        {
            Console.WriteLine($"\n🔀 Gateway: Routing reject request to PartnerService for order {id}");
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{_partnerServiceUrl}/api/orders/{id}/reject", reason);

            Console.WriteLine($"   ✅ Gateway: Reject request routed\n");
            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route reject for order {OrderId}", id);
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Mark order as ready (routes to PartnerService).
    /// </summary>
    [HttpPost("{id}/ready")]
    public async Task<IActionResult> MarkOrderReady(string id)
    {
        try
        {
            Console.WriteLine($"\n🔀 Gateway: Routing ready request to PartnerService for order {id}");
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"{_partnerServiceUrl}/api/orders/{id}/ready", null);

            Console.WriteLine($"   ✅ Gateway: Ready request routed\n");
            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route ready for order {OrderId}", id);
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Mark order as picked up (routes to PartnerService).
    /// </summary>
    [HttpPost("{id}/pickup")]
    public async Task<IActionResult> MarkOrderPickedUp(string id)
    {
        try
        {
            Console.WriteLine($"\n🔀 Gateway: Routing pickup request to PartnerService for order {id}");
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"{_partnerServiceUrl}/api/orders/{id}/pickup", null);

            Console.WriteLine($"   ✅ Gateway: Pickup request routed\n");
            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route pickup for order {OrderId}", id);
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Get pending orders (routes to PartnerService).
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingOrders()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_partnerServiceUrl}/api/orders/pending");

            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
                return Ok(orders);
            }

            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending orders");
            return StatusCode(500, "Gateway error");
        }
    }

    /// <summary>
    /// Gateway endpoint: Mark order as delivered (routes to PartnerService).
    /// </summary>
    [HttpPost("{id}/delivered")]
    public async Task<IActionResult> MarkOrderDelivered(string id)
    {
        try
        {
            Console.WriteLine($"\n🔀 Gateway: Routing delivered request to PartnerService for order {id}");
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync($"{_partnerServiceUrl}/api/orders/{id}/delivered", null);

            Console.WriteLine($"   ✅ Gateway: Delivered request routed\n");
            return StatusCode((int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route delivered for order {OrderId}", id);
            return StatusCode(500, "Gateway error");
        }
    }
}
