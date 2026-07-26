using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

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
}
