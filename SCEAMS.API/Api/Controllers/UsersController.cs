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
        var subject = User.FindFirstValue(
            JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(subject, out var userId) ||
            userId <= 0)
        {
            return Unauthorized(new
            {
                message = "Access token subject is invalid."
            });
        }

        var result = await _userService.GetCurrentUserAsync(
            userId,
            cancellationToken);

        return ToActionResult(result);
    }
}
