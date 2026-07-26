using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IHealthService
{
    Result<HealthResponseDto> GetStatus();
}
