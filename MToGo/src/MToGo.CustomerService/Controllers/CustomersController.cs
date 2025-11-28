using Microsoft.AspNetCore.Mvc;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.CustomerService.Services;
using MToGo.Shared.Models.Customer;

namespace MToGo.CustomerService.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateCustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] Customer request)
    {
        var sanitizedEmail = request.Email?.Replace("\r", "").Replace("\n", "");
        _logger.LogInformation("Registration request received for email: {Email}", sanitizedEmail);

        try
        {
            var result = await _customerService.RegisterCustomerAsync(request);
            return Created($"/api/v1/customers/{result.Id}", result);
        }
        catch (DuplicateEmailException ex)
        {
            _logger.LogWarning("Registration failed - duplicate email: {Email}", sanitizedEmail);
            return BadRequest(new { error = ex.Message });
        }
    }
}
