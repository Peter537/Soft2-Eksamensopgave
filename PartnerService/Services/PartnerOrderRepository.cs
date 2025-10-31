using PartnerService.Models;

namespace PartnerService.Services;

public class PartnerOrderRepository
{
    private readonly List<Order> _orders = new();

    public void AddOrder(Order order)
    {
        _orders.Add(order);
    }

    public Order? GetOrder(string orderId)
    {
        return _orders.FirstOrDefault(o => o.OrderId == orderId);
    }

    public List<Order> GetAllOrders()
    {
        return _orders;
    }

    public List<Order> GetPendingOrders()
    {
        return _orders.Where(o => o.Status == "Pending").ToList();
    }

    public void UpdateOrderStatus(string orderId, string newStatus, DateTime? timestamp = null)
    {
        var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
        if (order != null)
        {
            order.Status = newStatus;
            
            if (timestamp.HasValue)
            {
                if (newStatus == "Accepted") order.AcceptedAt = timestamp;
                else if (newStatus == "Ready") order.ReadyAt = timestamp;
                else if (newStatus == "PickedUp") order.PickedUpAt = timestamp;
            }
        }
    }
}
