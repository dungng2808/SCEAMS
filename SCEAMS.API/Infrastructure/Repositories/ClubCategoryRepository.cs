using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class ClubCategoryRepository
    : GenericRepository<ClubCategory>,
      IClubCategoryRepository
{
    public ClubCategoryRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<ClubCategoryResponseDto>>
        GetOrderedByNameAsync(
            CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new ClubCategoryResponseDto(
                category.Id,
                category.Name,
                category.Description))
            .ToListAsync(cancellationToken);
    }
}
