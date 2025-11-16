using OrderService.Models;

namespace OrderService.Services;

public class OrderRepository
{
    private readonly List<Order> _orders = new();
    private int _orderCounter = 1;

    public Order CreateOrder(string customerName, string deliveryAddress, List<string> items, decimal totalPrice)
    {
        var order = new Order
        {
            OrderId = $"ORDER-{DateTime.Now:yyyyMMdd}-{_orderCounter++:D3}",
            CustomerName = customerName,
            DeliveryAddress = deliveryAddress,
            OrderDetails = string.Join(", ", items),
            TotalPrice = totalPrice,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _orders.Add(order);
        return order;
    }

    public Order? GetOrder(string orderId)
    {
        return _orders.FirstOrDefault(o => o.OrderId == orderId);
    }

    public List<Order> GetAllOrders()
    {
        return _orders;
    }

    public void UpdateOrderStatus(string orderId, string newStatus)
    {
        var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null)
        {
            order.Status = newStatus;
        }
    }
}
