using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Authorize]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [ProducesResponseType<CurrentUserProfileResponseDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return InvalidTokenSubject();
        }

        var result = await _userService.GetCurrentUserAsync(
            userId,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPut("me")]
    [ProducesResponseType<CurrentUserProfileResponseDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCurrentUser(
        [FromBody] UpdateCurrentUserProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return InvalidTokenSubject();
        }

        var result = await _userService.UpdateCurrentUserAsync(
            userId,
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var subject = User.FindFirstValue(
            JwtRegisteredClaimNames.Sub);

        return int.TryParse(subject, out userId) &&
            userId > 0;
    }

    private IActionResult InvalidTokenSubject()
    {
        return Unauthorized(new
        {
            message = "Access token subject is invalid."
        });
    }
}
