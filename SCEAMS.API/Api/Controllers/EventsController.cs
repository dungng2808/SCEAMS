using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController : ApiControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableQuery(
        MaxTop = 50,
        PageSize = 50,
        AllowedQueryOptions = AllowedQueryOptions.Select |
            AllowedQueryOptions.Filter |
            AllowedQueryOptions.OrderBy |
            AllowedQueryOptions.Skip |
            AllowedQueryOptions.Top |
            AllowedQueryOptions.Expand |
            AllowedQueryOptions.Count)]
    [ProducesResponseType<IEnumerable<EventListResponseDto>>(StatusCodes.Status200OK)]
    public IActionResult GetEvents()
    {
        return Ok(_eventService.GetEventsQuery(User));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventByIdAsync(
            id,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.CreateEventAsync(
            request,
            User,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetEventById),
            new { id = result.Data!.Id },
            result.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organizer,Admin,Staff")]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] UpdateEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.UpdateEventAsync(
            id,
            request,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/submit")]
    [Authorize(Roles = "Organizer")]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitEvent(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.SubmitEventAsync(
            id,
            User,
            cancellationToken);

        return ToActionResult(result);
    }
}
