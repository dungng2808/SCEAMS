using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<PagedResult<UserListItemResponseDto>> GetPagedAsync(
        string? search,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

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

    Task<bool> EmailBelongsToOtherUserAsync(
        string email,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> StudentCodeBelongsToOtherUserAsync(
        string studentCode,
        int userId,
        CancellationToken cancellationToken = default);
}
