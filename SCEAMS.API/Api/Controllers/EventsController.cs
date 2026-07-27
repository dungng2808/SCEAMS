using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/events")]
[Produces("application/json", "application/xml")]
public sealed class EventsController : ApiControllerBase
{
    private readonly IEventService _eventService;
    private readonly IEventStatusSyncService _eventStatusSyncService;
    private readonly IRegistrationService _registrationService;
    private readonly IFeedbackService _feedbackService;
    private readonly IHostEnvironment _environment;

    public EventsController(
        IEventService eventService,
        IEventStatusSyncService eventStatusSyncService,
        IRegistrationService registrationService,
        IFeedbackService feedbackService,
        IHostEnvironment environment)
    {
        _eventService = eventService;
        _eventStatusSyncService = eventStatusSyncService;
        _registrationService = registrationService;
        _feedbackService = feedbackService;
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json", "application/xml")]
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
        var query = _eventService.GetEventsQuery(User);
        if (Request.Headers.TryGetValue("Accept", out var acceptValues) &&
            acceptValues.Any(value =>
                value.Contains("application/xml", StringComparison.OrdinalIgnoreCase)))
        {
            var serializer = new XmlSerializer(typeof(List<EventListResponseDto>));
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            serializer.Serialize(writer, query.ToList());
            return Content(writer.ToString(), "application/xml", Encoding.UTF8);
        }

        return Ok(query);
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

    [HttpGet("pending-approval")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<PagedResult<EventListResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingApprovalEvents(
        [FromQuery] int? clubId,
        [FromQuery] int? venueId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _eventService.GetPendingApprovalEventsAsync(
            clubId,
            venueId,
            from,
            to,
            page,
            pageSize,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<object>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveEvent(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.ApproveEventAsync(
            id,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectEvent(
        int id,
        [FromBody] RejectEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.RejectEventAsync(
            id,
            request,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Organizer,Admin,Staff")]
    [ProducesResponseType<EventDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEvent(
        int id,
        [FromBody] CancelEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.CancelEventAsync(
            id,
            request,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("sync-status")]
    [Authorize(Roles = "Admin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> SynchronizeEventStatuses(
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _eventStatusSyncService.SynchronizeAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/registrations")]
    [Authorize(Roles = "Admin,Organizer")]
    [ProducesResponseType<PagedResult<EventRegistrationListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventRegistrations(
        int id,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _registrationService.GetEventRegistrationsAsync(
            id,
            status,
            search,
            page,
            pageSize,
            User,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:int}/feedback")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType<FeedbackResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFeedback(
        int id,
        [FromBody] CreateFeedbackRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _feedbackService.CreateAsync(
            id,
            request,
            User,
            cancellationToken);
        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("{id:int}/feedback")]
    [AllowAnonymous]
    [ProducesResponseType<FeedbackSummaryResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeedback(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _feedbackService.GetSummaryAsync(
            id,
            page,
            pageSize,
            User,
            cancellationToken);

        return ToActionResult(result);
    }
}
