using System.ComponentModel.DataAnnotations;

namespace MToGo.PartnerService.Models;

public class CreateMenuItemRequest
{
    [Required(ErrorMessage = "Menu item name is required")]
    [MinLength(1, ErrorMessage = "Name cannot be empty")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
}
