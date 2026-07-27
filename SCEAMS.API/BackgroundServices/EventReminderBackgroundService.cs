using Microsoft.Extensions.Options;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Api.BackgroundServices;

public sealed class EventReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<EventReminderOptions> _options;
    private readonly ILogger<EventReminderBackgroundService> _logger;

    public EventReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EventReminderOptions> options,
        ILogger<EventReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Event reminder background service is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(
            Math.Clamp(_options.Value.IntervalSeconds, 30, 3600)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider
                .GetRequiredService<IEventReminderService>();
            var result = await service.RunAsync(cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation(
                    "Event reminder job scanned {Scanned}; sent {Sent}; skipped {Skipped}; failed {Failed}.",
                    result.Data!.Scanned,
                    result.Data.Sent,
                    result.Data.Skipped,
                    result.Data.Failed);
            }
            else
            {
                _logger.LogWarning("Event reminder job failed: {Message}", result.Message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Event reminder job crashed.");
        }
    }
}
