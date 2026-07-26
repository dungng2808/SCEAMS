using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/registrations")]
[Produces("application/json")]
public sealed class RegistrationsController : ApiControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationsController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType<RegistrationResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRegistrationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _registrationService.CreateAsync(
            request,
            User,
            cancellationToken);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType<RegistrationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _registrationService.CancelAsync(
            id,
            User,
            cancellationToken);

        return ToActionResult(result);
    }
}
