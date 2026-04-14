using MailingService.Entities;
using MailingService.Exceptions;
using MassTransit;
using Microsoft.Extensions.Options;

namespace MailingService.RateLimiting;

public class GoogleRateLimitFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly IRateLimitStateManager _stateManager;
    private readonly EmailQueueManager _queueManager;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<GoogleRateLimitFilter<T>> _logger;
    public GoogleRateLimitFilter(IRateLimitStateManager stateManager, EmailQueueManager queueManager, IOptions<RateLimitSettings> settings, ILogger<GoogleRateLimitFilter<T>> logger)
    {
        _stateManager = stateManager;
        _queueManager = queueManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        try
        {
            await next.Send(context);
        }
        catch (RateLimitException)
        {
            var banDuration = TimeSpan.FromMinutes(_settings.BanDurationMinutes);

            _logger.LogCritical("[Filter] Обнаружена ошибка 429! Откладываем письмо и выключаем очередь.");
            
            await context.Defer(banDuration);
            
            await _stateManager.BanUntilAsync(DateTime.UtcNow.Add(banDuration));
            
            _ = Task.Run(async () =>
            {
                await _queueManager.StopQueueAsync();
                _queueManager.ScheduleResume(banDuration);
            });
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("RateLimitFilter");
    }
}