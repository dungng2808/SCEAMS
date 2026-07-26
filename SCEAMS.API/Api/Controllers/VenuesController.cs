using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/venues")]
[Produces("application/json")]
public sealed class VenuesController : ApiControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<VenueResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateVenue(
        [FromBody] CreateVenueRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _venueService.CreateVenueAsync(
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<VenueResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateVenue(
        int id,
        [FromBody] UpdateVenueRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _venueService.UpdateVenueAsync(
            id,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/maintenance")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<VenueResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<object>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMaintenance(
        int id,
        [FromBody] UpdateVenueMaintenanceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _venueService.UpdateMaintenanceAsync(
            id,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteVenue(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _venueService.DeleteVenueAsync(
            id,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{id:int}/schedule")]
    [AllowAnonymous]
    [ProducesResponseType<VenueScheduleResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var includeInternalStatuses = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await _venueService.GetScheduleAsync(
            id,
            from,
            to,
            includeInternalStatuses,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<VenueResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenues(
        [FromQuery] string? search,
        [FromQuery] bool? maintenance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _venueService.GetVenuesAsync(
            search,
            maintenance,
            page,
            pageSize,
            cancellationToken);

        return ToActionResult(result);
    }
}
