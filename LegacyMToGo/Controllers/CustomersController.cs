using LegacyMToGo.Data;
using LegacyMToGo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LegacyMToGo.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController(LegacyContext dbContext, IConfiguration configuration) : ControllerBase
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

        // Generate real JWT token
        var token = GenerateJwtToken(customer);
        return Ok(new { jwt = token });
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

    private string GenerateJwtToken(Customer customer)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "MToGo-Super-Secret-Key-That-Should-Be-At-Least-256-Bits-Long-For-Security!";
        var issuer = jwtSettings["Issuer"] ?? "MToGo";
        var audience = jwtSettings["Audience"] ?? "MToGo-Services";
        var expirationMinutes = int.TryParse(jwtSettings["ExpirationMinutes"], out var exp) ? exp : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("userId", customer.Id.ToString()),
            new("email", customer.Email),
            new("role", "Customer"),
            new(ClaimTypes.Role, "Customer"),
            new("name", customer.Name)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
