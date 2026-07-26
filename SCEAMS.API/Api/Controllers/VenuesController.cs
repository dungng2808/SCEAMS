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
