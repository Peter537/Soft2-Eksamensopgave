using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.OrderService.Logging;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;
using MToGo.Shared.Security;

namespace MToGo.OrderService.Controllers
{
    [ApiController]
    [Route("orders")]
    [Authorize] // Require authentication by default
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IUserContextAccessor _userContextAccessor;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService, 
            IUserContextAccessor userContextAccessor,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _userContextAccessor = userContextAccessor;
            _logger = logger;
        }

        [HttpPost("order")]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
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
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AcceptOrder(int id, [FromBody] OrderAcceptRequest request)
        {
            _logger.ReceivedAcceptOrderRequest(id);

            var success = await _orderService.AcceptOrderAsync(id, request.EstimatedMinutes);

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

        [HttpPost("order/{id}/reject")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RejectOrder(int id, [FromBody] OrderRejectRequest? request = null)
        {
            _logger.ReceivedRejectOrderRequest(id);

            var success = await _orderService.RejectOrderAsync(id, request?.Reason);

            if (!success)
            {
                // Audit log
                _logger.RejectOrderFailed(id);
                return BadRequest();
            }

            // Audit log
            _logger.RejectOrderCompleted(id);

            return NoContent();
        }

        [HttpPost("order/{id}/set-ready")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SetReady(int id)
        {
            _logger.ReceivedSetReadyRequest(id);

            var success = await _orderService.SetReadyAsync(id);

            if (!success)
            {
                // Audit log
                _logger.SetReadyFailed(id);
                return BadRequest();
            }

            // Audit log
            _logger.SetReadyCompleted(id);

            return NoContent();
        }

        [HttpPost("order/{id}/assign-agent")]
        [Authorize(Policy = AuthorizationPolicies.AgentOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> AssignAgent(int id, [FromBody] AssignAgentRequest request)
        {
            var userContext = _userContextAccessor.UserContext;
            
            // Agent can only assign themselves
            if (userContext.UserId != request.AgentId)
            {
                return Forbid();
            }

            _logger.ReceivedAssignAgentRequest(id, request.AgentId);

            var result = await _orderService.AssignAgentAsync(id, request.AgentId);

            return result switch
            {
                AssignAgentResult.Success => NoContent(),
                AssignAgentResult.AgentAlreadyAssigned => Conflict(),
                _ => BadRequest()
            };
        }

        [HttpPost("order/{id}/pickup")]
        [Authorize(Policy = AuthorizationPolicies.AgentOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PickupOrder(int id)
        {
            _logger.ReceivedPickupOrderRequest(id);

            var result = await _orderService.PickupOrderAsync(id);

            switch (result)
            {
                case PickupResult.Success:
                    _logger.PickupOrderCompleted(id);
                    return NoContent();
                case PickupResult.NoAgentAssigned:
                    _logger.PickupOrderFailed(id);
                    return StatusCode(403);
                default:
                    _logger.PickupOrderFailed(id);
                    return BadRequest();
            }
        }

        [HttpPost("order/{id}/complete-delivery")]
        [Authorize(Policy = AuthorizationPolicies.AgentOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CompleteDelivery(int id)
        {
            _logger.ReceivedCompleteDeliveryRequest(id);

            var result = await _orderService.CompleteDeliveryAsync(id);

            switch (result)
            {
                case DeliveryResult.Success:
                    _logger.CompleteDeliveryCompleted(id);
                    return NoContent();
                case DeliveryResult.NoAgentAssigned:
                    _logger.CompleteDeliveryFailed(id);
                    return StatusCode(403);
                default:
                    _logger.CompleteDeliveryFailed(id);
                    return BadRequest();
            }
        }

        [HttpGet("customer/{id}")]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        [ProducesResponseType(typeof(List<CustomerOrderResponse>), 200)]
        public async Task<IActionResult> GetCustomerOrders(
            int id, 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            _logger.ReceivedGetCustomerOrdersRequest(id, startDate, endDate);

            var orders = await _orderService.GetOrdersByCustomerIdAsync(id, startDate, endDate);

            _logger.GetCustomerOrdersCompleted(id, orders.Count);

            return Ok(orders);
        }
    }
}