using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/club-categories")]
[Produces("application/json")]
public sealed class ClubCategoriesController : ApiControllerBase
{
    private readonly IClubCategoryService _clubCategoryService;

    public ClubCategoriesController(
        IClubCategoryService clubCategoryService)
    {
        _clubCategoryService = clubCategoryService;
    }

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<
        IReadOnlyList<ClubCategoryResponseDto>>(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClubCategories(
        CancellationToken cancellationToken)
    {
        var result = await _clubCategoryService
            .GetClubCategoriesAsync(cancellationToken);

        return ToActionResult(result);
    }
}
