namespace MToGo.PartnerService.Models;

/// <summary>
/// Response model for listing active partners (public endpoint)
/// </summary>
public class PublicPartnerResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

/// <summary>
/// Response model for a partner's public menu
/// </summary>
public class PublicMenuResponse
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<PublicMenuItemResponse> Items { get; set; } = new();
}

/// <summary>
/// Response model for a single public menu item
/// </summary>
public class PublicMenuItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
