using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MToGo.PartnerService.Models;

public class PartnerRegisterRequest
{
    [Required(ErrorMessage = "Name is required")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public required string Address { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Menu is required")]
    [MinLength(1, ErrorMessage = "At least one menu item is required")]
    public required List<MenuItemRequest> Menu { get; set; }
}
