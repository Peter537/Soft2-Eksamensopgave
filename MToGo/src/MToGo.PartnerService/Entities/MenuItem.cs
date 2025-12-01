namespace MToGo.PartnerService.Entities;

public class MenuItem
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Partner Partner { get; set; } = null!;
}
