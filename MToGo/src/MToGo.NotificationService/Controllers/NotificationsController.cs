using Microsoft.AspNetCore.Mvc;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;
using MToGo.NotificationService.Services;

namespace MToGo.NotificationService.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Send a notification to a customer (sms/push via Legacy Notification Service)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Notify([FromBody] NotificationRequest request)
    {
        try
        {
            var result = await _notificationService.SendNotificationAsync(request);
            return Accepted(result);
        }
        catch (CustomerNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (NotificationFailedException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
