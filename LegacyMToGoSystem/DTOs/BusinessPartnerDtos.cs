using System.ComponentModel.DataAnnotations;

namespace LegacyMToGoSystem.DTOs;

public class CreateBusinessPartnerDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Address { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string CuisineType { get; set; } = string.Empty;
    
    public List<MenuItemDto> MenuItems { get; set; } = new();
}

public class MenuItemDto
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, 10000)]
    public decimal Price { get; set; }
    
    [Required]
    public string Category { get; set; } = string.Empty;
    
    public bool IsAvailable { get; set; } = true;
}

public class BusinessPartnerResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MenuResponseDto
{
    public int BusinessPartnerId { get; set; }
    public string BusinessPartnerName { get; set; } = string.Empty;
    public List<MenuItemDto> MenuItems { get; set; } = new();
}
