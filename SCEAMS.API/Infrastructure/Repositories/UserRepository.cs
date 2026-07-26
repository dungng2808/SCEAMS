using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public sealed class UserRepository
    : GenericRepository<User>, IUserRepository
{
    public UserRepository(SceamsDbContext context)
        : base(context)
    {
    }

    public async Task<PagedResult<UserListItemResponseDto>>
        GetPagedAsync(
            string? search,
            UserRole? role,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.FullName.Contains(search) ||
                user.Email.Contains(search) ||
                user.StudentCode != null &&
                user.StudentCode.Contains(search) ||
                user.PhoneNumber != null &&
                user.PhoneNumber.Contains(search));
        }

        if (role.HasValue)
        {
            query = query.Where(user =>
                user.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(user =>
                user.IsActive == isActive.Value);
        }

        var totalItems = await query.CountAsync(
            cancellationToken);
        var items = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserListItemResponseDto(
                user.Id,
                user.FullName,
                user.Email,
                user.StudentCode,
                user.PhoneNumber,
                user.Role.ToString(),
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserListItemResponseDto>(
            items,
            totalItems);
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

    public async Task RevokeRefreshTokenAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(user =>
                user.RefreshTokenHash == refreshTokenHash)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        user => user.RefreshTokenHash,
                        (string?)null)
                    .SetProperty(
                        user => user.RefreshTokenExpiresAt,
                        (DateTime?)null),
                cancellationToken);
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

    public Task<bool> EmailBelongsToOtherUserAsync(
        string email,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            user =>
                user.Id != userId &&
                user.Email == email,
            cancellationToken);
    }

    public Task<bool> StudentCodeBelongsToOtherUserAsync(
        string studentCode,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            user =>
                user.Id != userId &&
                user.StudentCode == studentCode,
            cancellationToken);
    }

    public Task<int> CountActiveByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        return DbSet.CountAsync(
            user =>
                user.Role == role &&
                user.IsActive,
            cancellationToken);
    }
}
