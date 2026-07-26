using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> StudentCodeExistsAsync(
        string studentCode,
        CancellationToken cancellationToken = default);
}
