using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T>
    where T : class
{
    protected readonly SceamsDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(SceamsDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual Task<List<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual Task<List<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AddAsync(entity, cancellationToken)
            .AsTask();
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }
}
