using MailingService.Entities;
using MassTransit;
using Microsoft.Extensions.Options;

namespace MailingService.RateLimiting;

public class EmailQueueManager : IDisposable
{
    private readonly IBusControl _bus;
    private readonly IBusRegistrationContext _registrationContext;
    private readonly IRateLimitStateManager _stateManager;
    private readonly RabbitMqSettings _rabbitSettings;
    private readonly ILogger<EmailQueueManager> _logger;
    
    private readonly Action<IBusRegistrationContext, IReceiveEndpointConfigurator> _configureEndpoint;
    
    private HostReceiveEndpointHandle? _endpointHandle; 
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public EmailQueueManager(
        IBusControl bus, 
        IBusRegistrationContext registrationContext, 
        IRateLimitStateManager stateManager, 
        IOptions<RabbitMqSettings> rabbitSettings,
        ILogger<EmailQueueManager> logger,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator> configureEndpoint)
    {
        _bus = bus;
        _registrationContext = registrationContext;
        _stateManager = stateManager;
        _rabbitSettings = rabbitSettings.Value;
        _logger = logger;
        _configureEndpoint = configureEndpoint;
    }

    public async Task StartQueueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_endpointHandle is not null)
            {
                return;
            }
            
            _logger.LogInformation("[QueueManager] Подключение очереди '{Queue}'...", _rabbitSettings.QueueName);

            var handle = _bus.ConnectReceiveEndpoint(_rabbitSettings.QueueName, cfg =>
            {
                _configureEndpoint(_registrationContext, cfg);
            });

            await handle.Ready;
            _endpointHandle = handle;
        
            _logger.LogInformation("[QueueManager] Очередь '{Queue}' успешно запущена.", _rabbitSettings.QueueName);

        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopQueueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_endpointHandle is null)
            {
                return;
            }
            
            _logger.LogWarning("[QueueManager] Остановка очереди '{Queue}'...", _rabbitSettings.QueueName);
            
            await _endpointHandle.StopAsync();
            _endpointHandle = null;
            
            _logger.LogWarning("[QueueManager] Очередь остановлена.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ScheduleResume(TimeSpan delay)
    {
        _logger.LogWarning("[QueueManager] Таймер запущен. Очередь проснется через {Delay}.", delay);
        
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay);
                await _stateManager.ClearBanAsync();
                _logger.LogInformation("[QueueManager] Время бана вышло! Запускаем очередь...");
                await StartQueueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QueueManager] Ошибка при возобновлении очереди.");
            }
        });
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}