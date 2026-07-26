using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class ClubCategoryService : IClubCategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClubCategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<
        Result<IReadOnlyList<ClubCategoryResponseDto>>>
        GetClubCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.ClubCategories
            .GetOrderedByNameAsync(cancellationToken);

        return Result<IReadOnlyList<ClubCategoryResponseDto>>
            .Ok(categories);
    }
}
