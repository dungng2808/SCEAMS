using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs.Reports;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("event-summary")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<EventSummaryReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetEventSummaryAsync(
            from,
            to,
            User,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("club-activity")]
    [Authorize(Roles = "Admin,Staff,Organizer")]
    [ProducesResponseType<ClubActivityReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClubActivity(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetClubActivityAsync(
            from,
            to,
            User,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("attendance-rate")]
    [Authorize(Roles = "Admin,Staff,Organizer")]
    [ProducesResponseType<AttendanceRateReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAttendanceRate(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAttendanceRateAsync(
            from,
            to,
            User,
            cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("venue-usage")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<VenueUsageReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVenueUsage(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetVenueUsageAsync(
            from,
            to,
            User,
            cancellationToken);
        return ToActionResult(result);
    }
}
