namespace LegacyMToGoSystem.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int BusinessPartnerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? DeliveryAddress { get; set; }
    public int? AgentId { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PreparingAt { get; set; }
    public DateTime? AgentAssignedAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
