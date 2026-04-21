namespace MailingService.RateLimiting;

public class StartupEmailQueueHostedService : IHostedService 
{
    private readonly EmailQueueManager _queueManager;
    private readonly IRateLimitStateManager _stateManager;
    private readonly ILogger<StartupEmailQueueHostedService> _logger;

    public StartupEmailQueueHostedService(EmailQueueManager queueManager, IRateLimitStateManager stateManager, ILogger<StartupEmailQueueHostedService> logger)
    {
        _queueManager = queueManager;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var banExp = await _stateManager.GetBanExpirationAsync();

        if (banExp.HasValue && banExp.Value > DateTime.UtcNow)
        {
            var remaining = banExp.Value - DateTime.UtcNow;
            _logger.LogWarning("[Bootstrapper] Обнаружен активный бан! Очередь не запускается. Осталось спать: {Time}", remaining);
            
            _queueManager.ScheduleResume(remaining);
        }
        else
        {
            await _stateManager.ClearBanAsync();
            await _queueManager.StartQueueAsync();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _queueManager.StopQueueAsync();
    }
}