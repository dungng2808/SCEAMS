using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Api.Controllers;

[Route("api/clubs")]
[Produces("application/json")]
public sealed class ClubsController : ApiControllerBase
{
    private readonly IClubService _clubService;

    public ClubsController(IClubService clubService)
    {
        _clubService = clubService;
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableQuery(MaxTop = 50, PageSize = 50)]
    [ProducesResponseType<IEnumerable<ClubResponseDto>>(StatusCodes.Status200OK)]
    public IActionResult GetClubs()
    {
        var query = _clubService.GetClubsQuery(User);
        return Ok(query);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<ClubDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClubById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _clubService.GetClubByIdAsync(id, User, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Organizer)},{nameof(UserRole.Admin)}")]
    [ProducesResponseType<ClubDetailResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClub(
        [FromBody] CreateClubRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _clubService.CreateClubAsync(request, User, cancellationToken);
        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetClubById),
            new { id = result.Data!.Id },
            result.Data);
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Staff)}")]
    [ProducesResponseType<ClubDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveClub(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _clubService.ApproveClubAsync(id, User, cancellationToken);
        return ToActionResult(result);
    }
}
