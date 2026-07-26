using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

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

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<ClubCategoryResponseDto>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClubCategory(
        [FromBody] CreateClubCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _clubCategoryService
            .CreateClubCategoryAsync(
                request,
                cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<ClubCategoryResponseDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateClubCategory(
        int id,
        [FromBody] UpdateClubCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _clubCategoryService
            .UpdateClubCategoryAsync(
                id,
                request,
                cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteClubCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _clubCategoryService
            .DeleteClubCategoryAsync(
                id,
                cancellationToken);

        return ToActionResult(result);
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

