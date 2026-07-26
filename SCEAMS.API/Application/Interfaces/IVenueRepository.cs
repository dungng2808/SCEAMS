using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IVenueRepository : IGenericRepository<Venue>
{
    IQueryable<Venue> GetQueryable();
}
