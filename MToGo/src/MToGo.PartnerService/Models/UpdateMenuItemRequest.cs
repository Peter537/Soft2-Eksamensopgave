using System.ComponentModel.DataAnnotations;

namespace MToGo.PartnerService.Models;

public class UpdateMenuItemRequest
{
    [MinLength(1, ErrorMessage = "Name cannot be empty")]
    public string? Name { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }
}
