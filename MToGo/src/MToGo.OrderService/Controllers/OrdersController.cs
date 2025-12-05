using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;
using MToGo.Shared.Logging;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Context;

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
            _logger.LogInformation("Received CreateOrder request");

            var response = await _orderService.CreateOrderAsync(request);

            _logger.LogAuditInformation(
                action: "CreateOrderCompleted",
                resource: "Order",
                resourceId: response.Id.ToString(),
                message: "CreateOrder completed: OrderId={OrderId}",
                args: response.Id);

            return StatusCode(201, response);
        }

        [HttpPost("order/{id}/accept")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AcceptOrder(int id, [FromBody] OrderAcceptRequest request)
        {
            _logger.LogInformation("Received AcceptOrder request for OrderId: {OrderId}", id);

            var success = await _orderService.AcceptOrderAsync(id, request.EstimatedMinutes);

            if (!success)
            {
                _logger.LogAuditWarning(
                    action: "AcceptOrderFailed",
                    resource: "Order",
                    resourceId: id.ToString(),
                    message: "AcceptOrder failed: OrderId={OrderId}",
                    args: id);
                return BadRequest();
            }

            _logger.LogAuditInformation(
                action: "AcceptOrderCompleted",
                resource: "Order",
                resourceId: id.ToString(),
                message: "AcceptOrder completed: OrderId={OrderId}",
                args: id);

            return NoContent();
        }

        [HttpPost("order/{id}/reject")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RejectOrder(int id, [FromBody] OrderRejectRequest? request = null)
        {
            _logger.LogInformation("Received RejectOrder request for OrderId: {OrderId}", id);

            var success = await _orderService.RejectOrderAsync(id, request?.Reason);

            if (!success)
            {
                _logger.LogAuditWarning(
                    action: "RejectOrderFailed",
                    resource: "Order",
                    resourceId: id.ToString(),
                    message: "RejectOrder failed: OrderId={OrderId}",
                    args: id);
                return BadRequest();
            }

            _logger.LogAuditInformation(
                action: "RejectOrderCompleted",
                resource: "Order",
                resourceId: id.ToString(),
                message: "RejectOrder completed: OrderId={OrderId}",
                args: id);

            return NoContent();
        }

        [HttpPost("order/{id}/set-ready")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SetReady(int id)
        {
            _logger.LogInformation("Received SetReady request for OrderId: {OrderId}", id);

            var success = await _orderService.SetReadyAsync(id);

            if (!success)
            {
                _logger.LogAuditWarning(
                    action: "SetReadyFailed",
                    resource: "Order",
                    resourceId: id.ToString(),
                    message: "SetReady failed: OrderId={OrderId}",
                    args: id);
                return BadRequest();
            }

            _logger.LogAuditInformation(
                action: "SetReadyCompleted",
                resource: "Order",
                resourceId: id.ToString(),
                message: "SetReady completed: OrderId={OrderId}",
                args: id);

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
            if (userContext.Id != request.AgentId)
            {
                return Forbid();
            }

            _logger.LogInformation("Received AssignAgent request for OrderId: {OrderId}, AgentId: {AgentId}", id, request.AgentId);

            var result = await _orderService.AssignAgentAsync(id, request.AgentId);

            if (result == AssignAgentResult.Success)
            {
                _logger.LogAuditInformation(
                    action: "AssignAgentCompleted",
                    resource: "Order",
                    resourceId: id.ToString(),
                    userId: request.AgentId,
                    userRole: "Agent",
                    message: "AssignAgent completed: OrderId={OrderId}, AgentId={AgentId}",
                    args: new object[] { id, request.AgentId });
            }
            else
            {
                _logger.LogAuditWarning(
                    action: "AssignAgentFailed",
                    resource: "Order",
                    resourceId: id.ToString(),
                    userId: request.AgentId,
                    userRole: "Agent",
                    message: "AssignAgent failed: OrderId={OrderId}, AgentId={AgentId}, Result={Result}",
                    args: new object[] { id, request.AgentId, result });
            }

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
            _logger.LogInformation("Received PickupOrder request for OrderId: {OrderId}", id);

            var result = await _orderService.PickupOrderAsync(id);

            switch (result)
            {
                case PickupResult.Success:
                    _logger.LogAuditInformation(
                        action: "PickupOrderCompleted",
                        resource: "Order",
                        resourceId: id.ToString(),
                        message: "PickupOrder completed: OrderId={OrderId}",
                        args: id);
                    return NoContent();
                case PickupResult.NoAgentAssigned:
                    _logger.LogAuditWarning(
                        action: "PickupOrderFailed",
                        resource: "Order",
                        resourceId: id.ToString(),
                        message: "PickupOrder failed (no agent): OrderId={OrderId}",
                        args: id);
                    return StatusCode(403);
                default:
                    _logger.LogAuditWarning(
                        action: "PickupOrderFailed",
                        resource: "Order",
                        resourceId: id.ToString(),
                        message: "PickupOrder failed: OrderId={OrderId}",
                        args: id);
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
            _logger.LogInformation("Received CompleteDelivery request for OrderId: {OrderId}", id);

            var result = await _orderService.CompleteDeliveryAsync(id);

            switch (result)
            {
                case DeliveryResult.Success:
                    _logger.LogAuditInformation(
                        action: "CompleteDeliveryCompleted",
                        resource: "Order",
                        resourceId: id.ToString(),
                        message: "CompleteDelivery completed: OrderId={OrderId}",
                        args: id);
                    return NoContent();
                case DeliveryResult.NoAgentAssigned:
                    _logger.LogAuditWarning(
                        action: "CompleteDeliveryFailed",
                        resource: "Order",
                        resourceId: id.ToString(),
                        message: "CompleteDelivery failed (no agent): OrderId={OrderId}",
                        args: id);
                    return StatusCode(403);
                default:
                    _logger.LogAuditWarning(
                        action: "CompleteDeliveryFailed",
                        resource: "Order",
                        resourceId: id.ToString(),
                        message: "CompleteDelivery failed: OrderId={OrderId}",
                        args: id);
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
            _logger.LogInformation("Received GetCustomerOrders request for CustomerId: {CustomerId}, StartDate: {StartDate}, EndDate: {EndDate}", id, startDate, endDate);

            var orders = await _orderService.GetOrdersByCustomerIdAsync(id, startDate, endDate);

            _logger.LogInformation("GetCustomerOrders completed: CustomerId={CustomerId}, OrderCount={OrderCount}", id, orders.Count);

            return Ok(orders);
        }

        [HttpGet("customer/{id}/active")]
        [Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
        [ProducesResponseType(typeof(List<CustomerOrderResponse>), 200)]
        public async Task<IActionResult> GetActiveCustomerOrders(int id)
        {
            _logger.LogInformation("Received GetActiveCustomerOrders request for CustomerId: {CustomerId}", id);

            var orders = await _orderService.GetActiveOrdersByCustomerIdAsync(id);

            _logger.LogInformation("GetActiveCustomerOrders completed: CustomerId={CustomerId}, OrderCount={OrderCount}", id, orders.Count);

            return Ok(orders);
        }

        [HttpGet("agent/{id}")]
        [Authorize(Policy = AuthorizationPolicies.AgentOrManagement)]
        [ProducesResponseType(typeof(List<AgentDeliveryResponse>), 200)]
        public async Task<IActionResult> GetAgentDeliveries(
            int id, 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            _logger.LogInformation("Received GetAgentDeliveries request for AgentId: {AgentId}, StartDate: {StartDate}, EndDate: {EndDate}", id, startDate, endDate);

            var deliveries = await _orderService.GetOrdersByAgentIdAsync(id, startDate, endDate);

            _logger.LogInformation("GetAgentDeliveries completed: AgentId={AgentId}, DeliveryCount={DeliveryCount}", id, deliveries.Count);

            return Ok(deliveries);
        }

        [HttpGet("agent/{id}/active")]
        [Authorize(Policy = AuthorizationPolicies.AgentOrManagement)]
        [ProducesResponseType(typeof(List<AgentDeliveryResponse>), 200)]
        public async Task<IActionResult> GetActiveAgentOrders(int id)
        {
            _logger.LogInformation("Received GetActiveAgentOrders request for AgentId: {AgentId}", id);

            var orders = await _orderService.GetActiveOrdersByAgentIdAsync(id);

            _logger.LogInformation("GetActiveAgentOrders completed: AgentId={AgentId}, OrderCount={OrderCount}", id, orders.Count);

            return Ok(orders);
        }

        [HttpGet("partner/{id}")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(typeof(List<PartnerOrderResponse>), 200)]
        public async Task<IActionResult> GetPartnerOrders(
            int id, 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            _logger.LogInformation("Received GetPartnerOrders request for PartnerId: {PartnerId}, StartDate: {StartDate}, EndDate: {EndDate}", id, startDate, endDate);

            var orders = await _orderService.GetOrdersByPartnerIdAsync(id, startDate, endDate);

            _logger.LogInformation("GetPartnerOrders completed: PartnerId={PartnerId}, OrderCount={OrderCount}", id, orders.Count);

            return Ok(orders);
        }

        [HttpGet("partner/{id}/active")]
        [Authorize(Policy = AuthorizationPolicies.PartnerOnly)]
        [ProducesResponseType(typeof(List<PartnerOrderResponse>), 200)]
        public async Task<IActionResult> GetActivePartnerOrders(int id)
        {
            _logger.LogInformation("Received GetActivePartnerOrders request for PartnerId: {PartnerId}", id);

            var orders = await _orderService.GetActiveOrdersByPartnerIdAsync(id);

            _logger.LogInformation("GetActivePartnerOrders completed: PartnerId={PartnerId}, OrderCount={OrderCount}", id, orders.Count);

            return Ok(orders);
        }

        [HttpGet("order/{id}")]
        [Authorize(Policy = AuthorizationPolicies.AllAuthenticated)]
        [ProducesResponseType(typeof(OrderDetailResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            var userContext = _userContextAccessor.UserContext;

            if (!userContext.Id.HasValue || string.IsNullOrEmpty(userContext.Role))
            {
                return Forbid();
            }
            
            _logger.LogInformation("Received GetOrderDetail request for OrderId: {OrderId}, UserId: {UserId}, Role: {Role}", id, userContext.Id.Value, userContext.Role);

            var result = await _orderService.GetOrderDetailAsync(id, userContext.Id.Value, userContext.Role);

            if (!result.Success)
            {
                return result.Error switch
                {
                    GetOrderDetailError.NotFound => NotFound(),
                    GetOrderDetailError.Forbidden => Forbid(),
                    _ => BadRequest()
                };
            }

            _logger.LogInformation("GetOrderDetail completed: OrderId={OrderId}", id);

            return Ok(result.Order);
        }
    }
}