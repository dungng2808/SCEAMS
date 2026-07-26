using Microsoft.AspNetCore.Mvc;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.Controllers;

[Route("api/health")]
[Produces("application/json")]
public sealed class HealthController : ApiControllerBase
{
    private readonly IHealthService _healthService;
    private readonly IDatabaseHealthService _databaseHealthService;

    public HealthController(
        IHealthService healthService,
        IDatabaseHealthService databaseHealthService)
    {
        _healthService = healthService;
        _databaseHealthService = databaseHealthService;
    }

    [HttpGet]
    [ProducesResponseType<HealthResponseDto>(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var result = _healthService.GetStatus();
        return ToActionResult(result);
    }

    [HttpGet("database")]
    [ProducesResponseType<DatabaseHealthResponseDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabase(
        CancellationToken cancellationToken)
    {
        var result = await _databaseHealthService.GetStatusAsync(
            cancellationToken);

        return ToActionResult(result);
    }
}
