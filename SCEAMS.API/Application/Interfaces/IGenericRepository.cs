using System.Linq.Expressions;

namespace SCEAMS.Application.Interfaces;

public interface IGenericRepository<T>
    where T : class
{
    Task<List<T>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default);

    void Update(T entity);
    void Delete(T entity);
}
