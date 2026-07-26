using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class HealthService : IHealthService
{
    public Result<HealthResponseDto> GetStatus()
    {
        var version = typeof(HealthService).Assembly
            .GetName()
            .Version?
            .ToString() ?? "unknown";

        var response = new HealthResponseDto(
            Service: "SCEAMS.API",
            Version: version,
            Status: "Healthy");

        return Result<HealthResponseDto>.Ok(response);
    }
}
