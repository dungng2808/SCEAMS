namespace SCEAMS.MVC.Services.Authentication;

public sealed class RefreshTokenCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
