using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly RaigonDbContext _context;

    public NotificationsController(RaigonDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<NotificationListResponseData>>> GetNotifications()
    {
        var notifications = await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Time = n.TimeAgo,
                IsRead = n.IsRead,
                Type = n.Type
            })
            .ToListAsync();

        var unreadCount = notifications.Count(n => !n.IsRead);

        var data = new NotificationListResponseData
        {
            UnreadCount = unreadCount,
            Notifications = notifications
        };

        return Ok(ApiResponse<NotificationListResponseData>.Ok(data));
    }

    [HttpPatch("mark-all-read")]
    public async Task<ActionResult<ApiResponse>> MarkAllRead()
    {
        var unread = await _context.Notifications.Where(n => !n.IsRead).ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }
}
