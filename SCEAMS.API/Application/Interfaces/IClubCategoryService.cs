using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IClubCategoryService
{
    Task<Result<ClubCategoryResponseDto>>
        CreateClubCategoryAsync(
            CreateClubCategoryRequestDto request,
            CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ClubCategoryResponseDto>>>
        GetClubCategoriesAsync(
            CancellationToken cancellationToken = default);
}
