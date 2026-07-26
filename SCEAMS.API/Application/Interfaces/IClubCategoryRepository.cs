using SCEAMS.Application.DTOs;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IClubCategoryRepository
    : IGenericRepository<ClubCategory>
{
    Task<IReadOnlyList<ClubCategoryResponseDto>>
        GetOrderedByNameAsync(
            CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<bool> NameBelongsToOtherCategoryAsync(
        string normalizedName,
        int categoryId,
        CancellationToken cancellationToken = default);
}
