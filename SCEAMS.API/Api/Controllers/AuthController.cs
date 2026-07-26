using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<RegisteredStudentResponseDto>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterStudentRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterStudentAsync(
            request,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponseDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request,
            cancellationToken);

        return ToActionResult(result);
    }
}
