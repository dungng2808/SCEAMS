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
}
