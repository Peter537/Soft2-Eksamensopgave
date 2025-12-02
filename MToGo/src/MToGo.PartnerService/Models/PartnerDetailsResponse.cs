namespace MToGo.PartnerService.Models;

public class PartnerDetailsResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<MenuItemResponse> MenuItems { get; set; } = new();
}

public class MenuItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
