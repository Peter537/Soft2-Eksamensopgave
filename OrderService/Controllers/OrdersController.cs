using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;
using Shared.Kafka;
using Shared.Events;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderRepository _orderRepository;
    private readonly KafkaProducerService _kafkaProducer;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderRepository orderRepository, 
        KafkaProducerService kafkaProducer,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
            Console.WriteLine("│  📥 ORDERSERVICE: Processing new order                  │");
            Console.WriteLine("└─────────────────────────────────────────────────────────┘");
            Console.WriteLine($"   Customer: {request.CustomerName}");
            Console.WriteLine($"   Address: {request.DeliveryAddress}");
            Console.WriteLine($"   Items: {string.Join(", ", request.Items)}");
            Console.WriteLine($"   Total: {request.TotalPrice:C}");

            // 1. BUSINESS LOGIC: Save to database
            var order = _orderRepository.CreateOrder(
                request.CustomerName,
                request.DeliveryAddress,
                request.Items,
                request.TotalPrice
            );

            Console.WriteLine($"\n   ✅ Order saved to database: {order.OrderId}");
            _logger.LogInformation("✅ Order created in DB: {OrderId}", order.OrderId);

            // 2. BUSINESS LOGIC: Emit Kafka event after successful DB save
            Console.WriteLine($"   📤 Publishing to Kafka topic: {KafkaTopics.OrderCreated}");
            
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = order.OrderId,
                CustomerName = order.CustomerName,
                DeliveryAddress = order.DeliveryAddress,
                OrderDetails = order.OrderDetails,
                TotalPrice = order.TotalPrice,
                CreatedAt = order.CreatedAt
            };

            await _kafkaProducer.PublishAsync(KafkaTopics.OrderCreated, order.OrderId, orderEvent);

            Console.WriteLine($"   🎉 Event published! Notifications sent to:");
            Console.WriteLine($"      - NotificationService (customer notification)");
            Console.WriteLine($"      - WebsocketPartnerService (restaurant screen)\n");

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ ERROR: {ex.Message}\n");
            _logger.LogError(ex, "❌ Failed to create order");
            return StatusCode(500, "Failed to create order");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<Order> GetOrder(string id)
    {
        var order = _orderRepository.GetOrder(id);
        
        if (order == null)
            return NotFound($"Order {id} not found");

        return Ok(order);
    }

    [HttpGet]
    public ActionResult<List<Order>> GetAllOrders()
    {
        return Ok(_orderRepository.GetAllOrders());
    }
}
