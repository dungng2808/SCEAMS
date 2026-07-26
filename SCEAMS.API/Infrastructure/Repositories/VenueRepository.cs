using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class VenueRepository : GenericRepository<Venue>, IVenueRepository
{
    public VenueRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public IQueryable<Venue> GetQueryable()
    {
        return DbSet.AsNoTracking();
    }
}
