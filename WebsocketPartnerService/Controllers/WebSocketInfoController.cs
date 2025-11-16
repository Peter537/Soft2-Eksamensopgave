using Microsoft.AspNetCore.Mvc;
using WebsocketPartnerService.Services;

namespace WebsocketPartnerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebSocketInfoController : ControllerBase
{
    private readonly WebSocketConnectionManager _connectionManager;

    public WebSocketInfoController(WebSocketConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Health check endpoint to see how many restaurants are connected.
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            service = "WebsocketPartnerService",
            connectedRestaurants = _connectionManager.GetConnectedRestaurantCount(),
            websocketEndpoint = "/ws?restaurantId=YOUR_RESTAURANT_ID",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint to manually send a test message to a restaurant.
    /// Useful for debugging WebSocket connections.
    /// </summary>
    [HttpPost("test-send/{restaurantId}")]
    public async Task<IActionResult> TestSendMessage(string restaurantId, [FromBody] object message)
    {
        await _connectionManager.SendToRestaurant(restaurantId, new
        {
            type = "test_message",
            data = message,
            timestamp = DateTime.UtcNow
        });

        return Ok(new { message = $"Test message sent to {restaurantId}" });
    }
}
