using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<bool> TryRotateRefreshTokenAsync(
        int userId,
        string currentRefreshTokenHash,
        string replacementRefreshTokenHash,
        DateTime replacementExpiresAtUtc,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> StudentCodeExistsAsync(
        string studentCode,
        CancellationToken cancellationToken = default);
}
