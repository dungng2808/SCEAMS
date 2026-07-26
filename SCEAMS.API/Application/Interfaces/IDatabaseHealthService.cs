using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IDatabaseHealthService
{
    Task<Result<DatabaseHealthResponseDto>> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
