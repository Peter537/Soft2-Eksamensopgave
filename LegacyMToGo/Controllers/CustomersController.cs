using LegacyMToGo.Entities;
using LegacyMToGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyMToGo.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController(LegacyDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Creates a new legacy customer and returns its identifier.
    /// </summary>
    [HttpPost("post")]
    public async Task<ActionResult<object>> Create(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseMethod(request.NotificationMethod, out var method))
        {
            return BadRequest("Unsupported notification method");
        }

        var languagePref = LanguagePreference.En;
        if (!string.IsNullOrWhiteSpace(request.LanguagePreference))
        {
            _ = Enum.TryParse<LanguagePreference>(request.LanguagePreference, true, out languagePref);
        }

        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            DeliveryAddress = request.DeliveryAddress,
            NotificationMethod = method,
            Password = request.Password, // Password should already be hashed by CustomerService
            PhoneNumber = request.PhoneNumber,
            LanguagePreference = languagePref
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, new { id = customer.Id });
    }

    /// <summary>
    /// Returns customer data and hashed password so CustomerService can validate credentials.
    /// </summary>
    [HttpPost("post/login")]
    public async Task<ActionResult<object>> Login(CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken);
        if (customer is null || customer.IsDeleted)
        {
            return Unauthorized();
        }

        // Return customer data and hashed password for CustomerService to verify and generate JWT
        return Ok(new LegacyLoginResponse(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Password // Return hashed password for verification by CustomerService
        ));
    }

    /// <summary>
    /// Retrieves a customer profile by id if it is not deleted.
    /// </summary>
    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FindAsync([id], cancellationToken);
        return customer is null || customer.IsDeleted
            ? NotFound()
            : new CustomerResponse(
                customer.Name, 
                customer.DeliveryAddress, 
                customer.NotificationMethod.ToString(), 
                customer.PhoneNumber,
                customer.LanguagePreference.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Updates mutable customer fields and returns the latest profile snapshot.
    /// </summary>
    [HttpPatch("patch/{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, CustomerUpdateRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FindAsync([id], cancellationToken);
        if (customer is null || customer.IsDeleted)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            customer.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            customer.DeliveryAddress = request.DeliveryAddress;
        }

        if (!string.IsNullOrWhiteSpace(request.NotificationMethod))
        {
            if (!TryParseMethod(request.NotificationMethod, out var method))
            {
                return BadRequest("Unsupported notification method");
            }

            customer.NotificationMethod = method;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            customer.PhoneNumber = request.PhoneNumber;
        }

        if (!string.IsNullOrWhiteSpace(request.LanguagePreference))
        {
            if (Enum.TryParse<LanguagePreference>(request.LanguagePreference, true, out var langPref))
            {
                customer.LanguagePreference = langPref;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CustomerResponse(
            customer.Name, 
            customer.DeliveryAddress, 
            customer.NotificationMethod.ToString(), 
            customer.PhoneNumber,
            customer.LanguagePreference.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Soft-deletes a customer record and timestamps the deletion.
    /// </summary>
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FindAsync([id], cancellationToken);
        if (customer is null || customer.IsDeleted)
        {
            return NotFound();
        }

        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool TryParseMethod(string? value, out NotificationMethod method)
    {
        return Enum.TryParse(value, true, out method);
    }
}
