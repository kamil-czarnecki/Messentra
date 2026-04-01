using Microsoft.Extensions.Options;

namespace Messentra.Infrastructure.AutoUpdater;

public sealed class AutoUpdateCheckerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<AutoUpdatePollingOptions> _options;
    private readonly ILogger<AutoUpdateCheckerHostedService> _logger;
    private readonly TimeProvider _timeProvider;
    
    public AutoUpdateCheckerHostedService(
        IOptions<AutoUpdatePollingOptions> options,
        ILogger<AutoUpdateCheckerHostedService> logger,
        IServiceScopeFactory serviceScopeFactory,
        TimeProvider timeProvider)
    {
        _options = options;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        using var timer = new PeriodicTimer(options.CheckInterval, _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var scope = _serviceScopeFactory.CreateScope();
            var autoUpdater = scope.ServiceProvider.GetRequiredService<IAutoUpdaterService>();
            
            try
            {
                await autoUpdater.CheckForUpdates();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Periodic update check failed");
            }
        }
    }
}


