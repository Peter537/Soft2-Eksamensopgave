using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationRepository _repository;

        public NotificationsController(NotificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public ActionResult<List<Notification>> GetAllNotifications()
        {
            var notifications = _repository.GetAllNotifications();
            return Ok(notifications);
        }

        [HttpGet("{orderId}")]
        public ActionResult<List<Notification>> GetNotificationsByOrder(string orderId)
        {
            var notifications = _repository.GetNotificationsByOrder(orderId);
            return Ok(notifications);
        }

        [HttpGet("unread")]
        public ActionResult<List<Notification>> GetUnreadNotifications()
        {
            var notifications = _repository.GetUnreadNotifications();
            return Ok(notifications);
        }

        [HttpPost("{id}/mark-read")]
        public ActionResult MarkAsRead(string id)
        {
            _repository.MarkAsRead(id);
            return Ok();
        }
    }
}
