using Microsoft.AspNetCore.Mvc;
using PartnerService.Services;
using Shared.Kafka;
using Shared.Events;

namespace PartnerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly PartnerOrderRepository _orderRepository;
    private readonly KafkaProducerService _kafkaProducer;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        PartnerOrderRepository orderRepository,
        KafkaProducerService kafkaProducer,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptOrder(string id)
    {
        var order = _orderRepository.GetOrder(id);
        if (order == null)
            return NotFound($"Order {id} not found");

        Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  ✅ RESTAURANT ACCEPTED ORDER                           │");
        Console.WriteLine("└─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"   Order ID: {id}");

        var acceptedAt = DateTime.UtcNow;
        _orderRepository.UpdateOrderStatus(id, "Accepted", acceptedAt);

        Console.WriteLine($"   ✅ Status updated to 'Accepted' in database");
        Console.WriteLine($"   📤 Publishing to Kafka topic: {KafkaTopics.OrderAccepted}");

        var acceptedEvent = new OrderAcceptedEvent
        {
            OrderId = id,
            RestaurantId = "RESTAURANT-001",
            AcceptedAt = acceptedAt
        };

        await _kafkaProducer.PublishAsync(KafkaTopics.OrderAccepted, id, acceptedEvent);

        Console.WriteLine($"   🎉 Customer will be notified!\n");

        return Ok(new { message = "Order accepted", orderId = id });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectOrder(string id, [FromBody] string reason)
    {
        var order = _orderRepository.GetOrder(id);
        if (order == null)
            return NotFound($"Order {id} not found");

        Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  ❌ RESTAURANT REJECTED ORDER                           │");
        Console.WriteLine("└─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"   Order ID: {id}");
        Console.WriteLine($"   Reason: {reason}");

        var rejectedAt = DateTime.UtcNow;
        _orderRepository.UpdateOrderStatus(id, "Rejected", rejectedAt);

        Console.WriteLine($"   ✅ Status updated to 'Rejected' in database");
        Console.WriteLine($"   📤 Publishing to Kafka topic: {KafkaTopics.OrderRejected}");

        var rejectedEvent = new OrderRejectedEvent
        {
            OrderId = id,
            RestaurantId = "RESTAURANT-001",
            Reason = reason,
            RejectedAt = rejectedAt
        };

        await _kafkaProducer.PublishAsync(KafkaTopics.OrderRejected, id, rejectedEvent);

        Console.WriteLine($"   🎉 Customer will be notified!\n");

        return Ok(new { message = "Order rejected", orderId = id });
    }

    [HttpPost("{id}/prepare")]
    public async Task<IActionResult> StartPreparing(string id)
    {
        var order = _orderRepository.GetOrder(id);
        if (order == null)
            return NotFound($"Order {id} not found");

        Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  👨‍🍳 RESTAURANT STARTED PREPARING                        │");
        Console.WriteLine("└─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"   Order ID: {id}");

        var startedAt = DateTime.UtcNow;
        _orderRepository.UpdateOrderStatus(id, "Preparing", startedAt);

        Console.WriteLine($"   ✅ Status updated to 'Preparing' in database");
        Console.WriteLine($"   📤 Publishing to Kafka topic: {KafkaTopics.OrderPreparing}");

        var preparingEvent = new OrderPreparingEvent
        {
            OrderId = id,
            StartedAt = startedAt
        };

        await _kafkaProducer.PublishAsync(KafkaTopics.OrderPreparing, id, preparingEvent);

        Console.WriteLine($"   🎉 Customer notified: 'Restaurant is preparing your food!'\n");

        return Ok(new { message = "Order preparation started", orderId = id });
    }

    [HttpPost("{id}/ready")]
    public async Task<IActionResult> MarkReady(string id)
    {
        var order = _orderRepository.GetOrder(id);
        if (order == null)
            return NotFound($"Order {id} not found");

        Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  ✅ ORDER READY FOR PICKUP                              │");
        Console.WriteLine("└─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"   Order ID: {id}");

        var readyAt = DateTime.UtcNow;
        _orderRepository.UpdateOrderStatus(id, "Ready", readyAt);

        Console.WriteLine($"   ✅ Status updated to 'Ready' in database");
        Console.WriteLine($"   📤 Publishing to Kafka topic: {KafkaTopics.OrderReady}");

        var readyEvent = new OrderReadyEvent
        {
            OrderId = id,
            ReadyAt = readyAt
        };

        await _kafkaProducer.PublishAsync(KafkaTopics.OrderReady, id, readyEvent);

        Console.WriteLine($"   🎉 Driver will be notified to pickup!\n");

        return Ok(new { message = "Order ready for pickup", orderId = id });
    }

    [HttpPost("{id}/pickup")]
    public async Task<IActionResult> MarkPickedUp(string id)
    {
        var order = _orderRepository.GetOrder(id);
        if (order == null)
            return NotFound($"Order {id} not found");

        Console.WriteLine("\n┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  🚗 DRIVER PICKED UP ORDER                              │");
        Console.WriteLine("└─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"   Order ID: {id}");

        var pickedUpAt = DateTime.UtcNow;
        _orderRepository.UpdateOrderStatus(id, "PickedUp", pickedUpAt);

        Console.WriteLine($"   ✅ Status updated to 'PickedUp' in database");
        Console.WriteLine($"   📤 Publishing to Kafka topic: {KafkaTopics.OrderPickedUp}");

        var pickedUpEvent = new OrderPickedUpEvent
        {
            OrderId = id,
            DriverId = "DRIVER-001",
            PickedUpAt = pickedUpAt
        };

        await _kafkaProducer.PublishAsync(KafkaTopics.OrderPickedUp, id, pickedUpEvent);

        Console.WriteLine($"   🎉 GPS tracking will start!");
        Console.WriteLine($"   🎉 Customer notified: 'Driver is on the way!'\n");

        return Ok(new { message = "Order picked up", orderId = id });
    }

    [HttpGet]
    public IActionResult GetAllOrders()
    {
        return Ok(_orderRepository.GetAllOrders());
    }

    [HttpGet("pending")]
    public IActionResult GetPendingOrders()
    {
        return Ok(_orderRepository.GetPendingOrders());
    }
}
