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
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Email == email,
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
}
