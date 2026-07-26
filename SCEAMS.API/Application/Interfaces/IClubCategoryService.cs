using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IClubCategoryService
{
    Task<Result<IReadOnlyList<ClubCategoryResponseDto>>>
        GetClubCategoriesAsync(
            CancellationToken cancellationToken = default);
}
