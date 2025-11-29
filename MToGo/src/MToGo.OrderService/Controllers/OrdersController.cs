using Microsoft.AspNetCore.Mvc;
using MToGo.OrderService.Logging;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;

namespace MToGo.OrderService.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("order")]
        [ProducesResponseType(typeof(OrderCreateResponse), 201)]
        public async Task<IActionResult> CreateOrder(OrderCreateRequest request)
        {
            _logger.ReceivedCreateOrderRequest();

            var response = await _orderService.CreateOrderAsync(request);

            // Audit log
            _logger.CreateOrderCompleted(response.Id);

            return StatusCode(201, response);
        }

        [HttpPost("order/{id}/accept")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            _logger.ReceivedAcceptOrderRequest(id);

            var success = await _orderService.AcceptOrderAsync(id);

            if (!success)
            {
                // Audit log
                _logger.AcceptOrderFailed(id);
                return BadRequest();
            }

            // Audit log
            _logger.AcceptOrderCompleted(id);

            return NoContent();
        }
    }
}