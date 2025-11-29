using LegacyMToGo.Data;
using LegacyMToGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyMToGo.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController(LegacyContext dbContext) : ControllerBase
{
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
            Password = HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            LanguagePreference = languagePref
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, new { id = customer.Id });
    }

    [HttpPost("post/login")]
    public async Task<ActionResult<object>> Login(CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken);
        if (customer is null || !VerifyPassword(request.Password, customer.Password))
        {
            return Unauthorized();
        }

        // This is a legacy system, so we emulate the issuance of a JWT token.
        return Ok(new { jwt = $"legacy-token-{customer.Id}" });
    }

    [HttpGet("get/{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FindAsync([id], cancellationToken);
        return customer is null
            ? NotFound()
            : new CustomerResponse(
                customer.Name, 
                customer.DeliveryAddress, 
                customer.NotificationMethod.ToString(), 
                customer.PhoneNumber,
                customer.LanguagePreference.ToString().ToLowerInvariant());
    }

    [HttpPatch("patch/{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, CustomerUpdateRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FindAsync([id], cancellationToken);
        if (customer is null)
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

    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FindAsync([id], cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private static string HashPassword(string value)
    {
        return BCrypt.Net.BCrypt.HashPassword(value ?? string.Empty, workFactor: 12);
    }


    private static bool VerifyPassword(string? plaintext, string hashed) 
    {
        return !string.IsNullOrEmpty(plaintext) && BCrypt.Net.BCrypt.Verify(plaintext, hashed);
    }

    private static bool TryParseMethod(string? value, out NotificationMethod method)
    {
        return Enum.TryParse(value, true, out method);
    }
}

public record CustomerCreateRequest(string Name, string Email, string DeliveryAddress, string NotificationMethod, string Password, string PhoneNumber, string? LanguagePreference = "en");
public record CustomerLoginRequest(string Email, string Password);
public record CustomerUpdateRequest(string? Name, string? DeliveryAddress, string? NotificationMethod, string? PhoneNumber, string? LanguagePreference);
public record CustomerResponse(string Name, string DeliveryAddress, string NotificationMethod, string? PhoneNumber, string? LanguagePreference);
