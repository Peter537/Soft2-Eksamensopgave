using System.ComponentModel.DataAnnotations;

namespace LegacyMToGoSystem.DTOs;

public class CreateOrderDto
{
    [Required]
    public int CustomerId { get; set; }
    
    [Required]
    public int BusinessPartnerId { get; set; }
    
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<OrderItemDto> Items { get; set; } = new();
    
    public string? DeliveryAddress { get; set; }
}

public class OrderItemDto
{
    [Required]
    public int MenuItemId { get; set; }
    
    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; }
}

public class OrderResponseDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int BusinessPartnerId { get; set; }
    public string BusinessPartnerName { get; set; } = string.Empty;
    public List<OrderItemDetailDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal FeePercentage { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public int? AgentId { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PreparingAt { get; set; }
    public DateTime? AgentAssignedAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class OrderItemDetailDto
{
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
