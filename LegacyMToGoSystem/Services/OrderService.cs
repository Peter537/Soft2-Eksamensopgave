using LegacyMToGoSystem.DTOs;
using LegacyMToGoSystem.Infrastructure.Messaging;
using LegacyMToGoSystem.Models;
using LegacyMToGoSystem.Repositories;
using System.Text.Json;

namespace LegacyMToGoSystem.Services;

public interface IOrderService
{
    Task<OrderResponseDto> PlaceOrderAsync(CreateOrderDto dto);
    Task<OrderResponseDto?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<OrderResponseDto>> GetCustomerOrdersAsync(int customerId);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBusinessPartnerRepository _businessPartnerRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly RabbitMQProducer _rabbitMQ;
    private readonly KafkaProducer _kafka;

    public OrderService(
        IOrderRepository orderRepository,
        IBusinessPartnerRepository businessPartnerRepository,
        ICustomerRepository customerRepository,
        RabbitMQProducer rabbitMQ,
        KafkaProducer kafka)
    {
        _orderRepository = orderRepository;
        _businessPartnerRepository = businessPartnerRepository;
        _customerRepository = customerRepository;
        _rabbitMQ = rabbitMQ;
        _kafka = kafka;
    }

    public async Task<OrderResponseDto> PlaceOrderAsync(CreateOrderDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);
        if (customer == null)
            throw new Exception($"Customer {dto.CustomerId} not found");

        var partner = await _businessPartnerRepository.GetByIdAsync(dto.BusinessPartnerId);
        if (partner == null)
            throw new Exception($"Business partner {dto.BusinessPartnerId} not found");

        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var itemDto in dto.Items)
        {
            var menuItem = partner.MenuItems.FirstOrDefault(m => m.Id == itemDto.MenuItemId);
            if (menuItem == null)
                throw new Exception($"Menu item {itemDto.MenuItemId} not found");

            if (!menuItem.IsAvailable)
                throw new Exception($"Menu item {menuItem.Name} is not available");

            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.Id,
                MenuItemName = menuItem.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price,
                TotalPrice = menuItem.Price * itemDto.Quantity
            };

            orderItems.Add(orderItem);
            subtotal += orderItem.TotalPrice;
        }

        var feePercentage = CalculateFeePercentage(subtotal);
        var feeAmount = subtotal * (feePercentage / 100);
        var totalAmount = subtotal + feeAmount;

        var order = new Order
        {
            CustomerId = dto.CustomerId,
            BusinessPartnerId = dto.BusinessPartnerId,
            Items = orderItems,
            DeliveryAddress = dto.DeliveryAddress,
            SubTotal = subtotal,
            FeePercentage = feePercentage,
            FeeAmount = feeAmount,
            TotalAmount = totalAmount,
            Status = OrderStatus.Placed
        };

        var created = await _orderRepository.CreateAsync(order);

        _rabbitMQ.PublishMessage("orders", JsonSerializer.Serialize(new
        {
            orderId = created.Id,
            customerId = created.CustomerId,
            totalAmount = created.TotalAmount,
            status = created.Status.ToString()
        }));

        await _kafka.PublishEventAsync("order-events", "OrderPlaced", new
        {
            orderId = created.Id,
            customerId = created.CustomerId,
            businessPartnerId = created.BusinessPartnerId,
            totalAmount = created.TotalAmount,
            timestamp = created.PlacedAt
        });

        _ = Task.Run(async () => await ProcessOrderAsync(created.Id));

        return MapToResponseDto(created);
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return order == null ? null : MapToResponseDto(order);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetCustomerOrdersAsync(int customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        return orders.Select(MapToResponseDto);
    }

    private async Task ProcessOrderAsync(int orderId)
    {
        await Task.Delay(1000);
        await UpdateOrderStatus(orderId, OrderStatus.PaymentProcessing);

        _rabbitMQ.PublishMessage("payments", JsonSerializer.Serialize(new
        {
            orderId,
            action = "process_payment"
        }));

        await Task.Delay(2000);
        await UpdateOrderStatus(orderId, OrderStatus.Paid);

        await Task.Delay(5000);
        await UpdateOrderStatus(orderId, OrderStatus.Preparing);

        await _kafka.PublishEventAsync("order-events", "OrderPreparing", new { orderId, timestamp = DateTime.UtcNow });

        await Task.Delay(5000);
        await UpdateOrderStatus(orderId, OrderStatus.AgentAssigned);

        await _kafka.PublishEventAsync("order-events", "AgentAssigned", new { orderId, timestamp = DateTime.UtcNow });

        await UpdateOrderStatus(orderId, OrderStatus.InTransit);

        await _kafka.PublishEventAsync("order-events", "OrderInTransit", new { orderId, timestamp = DateTime.UtcNow });

        await Task.Delay(2000);
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order != null)
        {
            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = order.InTransitAt?.AddMinutes(20) ?? DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            await _kafka.PublishEventAsync("order-events", "OrderDelivered", new { orderId, timestamp = order.DeliveredAt });
        }
    }

    private async Task UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) return;

        order.Status = status;

        switch (status)
        {
            case OrderStatus.PaymentProcessing:
                break;
            case OrderStatus.Paid:
                order.PaidAt = DateTime.UtcNow;
                break;
            case OrderStatus.Preparing:
                order.PreparingAt = DateTime.UtcNow;
                break;
            case OrderStatus.AgentAssigned:
                order.AgentAssignedAt = DateTime.UtcNow;
                break;
            case OrderStatus.InTransit:
                order.InTransitAt = DateTime.UtcNow;
                break;
        }

        await _orderRepository.UpdateAsync(order);
    }

    private decimal CalculateFeePercentage(decimal subtotal)
    {
        if (subtotal <= 100) return 6.0m;
        if (subtotal >= 1000) return 3.0m;

        var range = 1000m - 100m;
        var position = subtotal - 100m;
        var percentage = 6.0m - (3.0m * (position / range));

        return Math.Round(percentage, 2);
    }

    private OrderResponseDto MapToResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            BusinessPartnerId = order.BusinessPartnerId,
            Items = order.Items.Select(i => new OrderItemDetailDto
            {
                MenuItemId = i.MenuItemId,
                MenuItemName = i.MenuItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            DeliveryAddress = order.DeliveryAddress,
            SubTotal = order.SubTotal,
            FeePercentage = order.FeePercentage,
            FeeAmount = order.FeeAmount,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            PlacedAt = order.PlacedAt,
            PaidAt = order.PaidAt,
            PreparingAt = order.PreparingAt,
            AgentAssignedAt = order.AgentAssignedAt,
            InTransitAt = order.InTransitAt,
            DeliveredAt = order.DeliveredAt
        };
    }
}
