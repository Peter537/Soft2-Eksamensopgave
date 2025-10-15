using LegacyMToGoSystem.DTOs;
using LegacyMToGoSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyMToGoSystem.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> PlaceOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _service.PlaceOrderAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id)
    {
        var order = await _service.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound($"Order {id} not found");

        return Ok(order);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetCustomerOrders(int customerId)
    {
        var orders = await _service.GetCustomerOrdersAsync(customerId);
        return Ok(orders);
    }
}
