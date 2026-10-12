using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BuildingManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;

    public NotificationsController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationInfo>>> GetRecent([FromQuery] int limit = 30) =>
        Ok(await Grain().GetRecentAsync(Math.Clamp(limit, 1, 100)));

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount() =>
        Ok(await Grain().GetUnreadCountAsync());

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId)
    {
        await Grain().MarkReadAsync(notificationId);
        return Ok();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await Grain().MarkAllReadAsync();
        return Ok();
    }

        private INotificationGrain Grain()
    {
        var email = User.FindFirst("email")?.Value?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException("The token has no email claim.");
        return _grainFactory.GetGrain<INotificationGrain>(email);
    }
}
