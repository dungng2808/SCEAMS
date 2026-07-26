using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class ClubRepository
    : GenericRepository<Club>, IClubRepository
{
    public ClubRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public Task<Club?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .Include(club => club.Category)
            .Include(club => club.CreatedByUser)
            .SingleOrDefaultAsync(
                club => club.Id == id,
                cancellationToken);
    }

    public IQueryable<Club> GetQueryable()
    {
        return DbSet.AsNoTracking();
    }
}

