using LegacyMToGo.Entities;
using LegacyMToGo.Models;
using LegacyMToGo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegacyMToGo.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController(LegacyDbContext dbContext, INotificationDispatcher dispatcher, ILogger<NotificationsController> logger) : ControllerBase
{
    /// <summary>
    /// Sends a notification to a customer using their preferred channel.
    /// </summary>
    [HttpPost("notify")]
    public async Task<IActionResult> Notify(NotificationRequest request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var destination = customer.NotificationMethod switch
        {
            NotificationMethod.Email => customer.Email,
            _ => customer.PhoneNumber ?? customer.Email
        };

        await dispatcher.DispatchAsync(customer.NotificationMethod, destination, request.Message, cancellationToken);
        logger.LogInformation("Legacy notification dispatched to customer {CustomerId}", request.CustomerId);
        return Accepted();
    }
}
