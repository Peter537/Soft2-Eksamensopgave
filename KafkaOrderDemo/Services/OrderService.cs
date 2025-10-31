namespace KafkaOrderDemo.Services;

public class OrderService
{
    private readonly List<Order> _orders = new();
    private int _orderCounter = 1;

    public event Action? OnOrdersChanged;

    public List<Order> GetAllOrders() => _orders;

    public List<Order> GetPendingOrders() => _orders.Where(o => o.Status == "Pending").ToList();

    public List<Order> GetActiveOrders() => _orders.Where(o => o.Status != "Pending" && o.Status != "Rejected" && o.Status != "Delivered").ToList();

    public List<Order> GetPickedUpOrders() => _orders.Where(o => o.Status == "PickedUp").ToList();

    public void CreateOrder(string customerName, string deliveryAddress, List<string> items, decimal totalPrice)
    {
        var order = new Order
        {
            OrderId = $"ORDER-{DateTime.Now:yyyyMMdd}-{_orderCounter:D3}",
            CustomerName = customerName,
            DeliveryAddress = deliveryAddress,
            OrderDetails = string.Join(", ", items),
            TotalPrice = totalPrice,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        _orderCounter++;
        _orders.Add(order);
        OnOrdersChanged?.Invoke();
    }

    public void AcceptOrder(string orderId)
    {
        var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null)
        {
            order.Status = "New";
            OnOrdersChanged?.Invoke();
        }
    }

    public void RejectOrder(string orderId)
    {
        var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null)
        {
            order.Status = "Rejected";
            OnOrdersChanged?.Invoke();
        }
    }

    public void UpdateOrderStatus(string orderId, string newStatus)
    {
        var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null)
        {
            order.Status = newStatus;
            OnOrdersChanged?.Invoke();
        }
    }
}

public class Order
{
    public string OrderId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string OrderDetails { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; }
}
