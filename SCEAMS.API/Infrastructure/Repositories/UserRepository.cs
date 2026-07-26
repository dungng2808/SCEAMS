using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class UserRepository
    : GenericRepository<User>, IUserRepository
{
    public UserRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .SingleOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public Task<User?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user =>
                    user.RefreshTokenHash == refreshTokenHash,
                cancellationToken);
    }

    public async Task<bool> TryRotateRefreshTokenAsync(
        int userId,
        string currentRefreshTokenHash,
        string replacementRefreshTokenHash,
        DateTime replacementExpiresAtUtc,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbSet
            .Where(user =>
                user.Id == userId &&
                user.IsActive &&
                user.RefreshTokenHash ==
                    currentRefreshTokenHash &&
                user.RefreshTokenExpiresAt > utcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        user => user.RefreshTokenHash,
                        replacementRefreshTokenHash)
                    .SetProperty(
                        user => user.RefreshTokenExpiresAt,
                        replacementExpiresAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }

    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public Task<bool> StudentCodeExistsAsync(
        string studentCode,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            user => user.StudentCode == studentCode,
            cancellationToken);
    }
}
