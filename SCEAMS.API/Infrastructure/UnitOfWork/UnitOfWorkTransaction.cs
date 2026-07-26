using Microsoft.EntityFrameworkCore.Storage;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Infrastructure.UnitOfWork;

public sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;

    public UnitOfWorkTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}
