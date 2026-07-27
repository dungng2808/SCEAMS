using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/notifications")]
[ApiController]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationLogStore _logStore;
    private readonly IHostEnvironment _environment;

    public NotificationsController(
        INotificationLogStore logStore,
        IHostEnvironment environment)
    {
        _logStore = logStore;
        _environment = environment;
    }

    [HttpGet("logs")]
    [Authorize(Roles = "Admin,Staff")]
    public IActionResult GetLogs(
        int? eventId,
        string? notificationType,
        bool? success,
        int limit = 100)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var entries = _logStore.GetRecent(limit)
            .Where(entry => !eventId.HasValue || entry.EventId == eventId.Value)
            .Where(entry => string.IsNullOrWhiteSpace(notificationType) ||
                entry.NotificationType.Equals(notificationType, StringComparison.OrdinalIgnoreCase))
            .Where(entry => !success.HasValue || entry.IsSuccess == success.Value)
            .ToList();
        return Ok(entries);
    }
}
