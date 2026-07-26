using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IClubRepository : IGenericRepository<Club>
{
    Task<Club?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);
}
