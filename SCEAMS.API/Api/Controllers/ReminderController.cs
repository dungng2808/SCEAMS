using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Api.BackgroundServices;

namespace SCEAMS.Api.Controllers;

[Route("api/reminders")]
[ApiController]
public sealed class ReminderController : ControllerBase
{
    private readonly EventReminderBackgroundService _backgroundService;
    private readonly IHostEnvironment _environment;

    public ReminderController(
        EventReminderBackgroundService backgroundService,
        IHostEnvironment environment)
    {
        _backgroundService = backgroundService;
        _environment = environment;
    }

    [HttpPost("run")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _backgroundService.RunOnceAsync(cancellationToken);
        return result is null
            ? Ok(new { message = "Reminder job không hoàn tất." })
            : Ok(result);
    }
}
