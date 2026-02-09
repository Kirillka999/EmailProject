using MailingService.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;

namespace MailingService.Services;

public class SmtpConnectionManager : IDisposable
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpConnectionManager> _logger;
    
    private readonly SmtpClient _client = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Timer? _idleTimer;
    private readonly TimeSpan _timeout;

    public SmtpConnectionManager(IOptions<SmtpSettings> settings, ILogger<SmtpConnectionManager> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _timeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds);
    }
    
    public async Task ExecuteAsync(Func<SmtpClient, Task> action)
    {
        await _lock.WaitAsync();
        
        try
        {
            StopIdleTimer();

            if (!_client.IsConnected)
            {
                _logger.LogInformation("[ConnectionManager] Connecting...");
                
                await _client.ConnectAsync(_settings.Host, _settings.Port, _settings.EnableSsl);
                await _client.AuthenticateAsync(_settings.Login, _settings.Password);
            }
            
            await action(_client);

            StartIdleTimer();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void StartIdleTimer()
    {
        _idleTimer?.Dispose();
        _idleTimer = new Timer(DisconnectCallback, null, _timeout, Timeout.InfiniteTimeSpan);
    }

    private void StopIdleTimer()
    {
        _idleTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void DisconnectCallback(object? state)
    {
        if (_lock.Wait(0))
        {
            try
            {
                if (_client.IsConnected)
                {
                    _logger.LogInformation("[ConnectionManager] Timeout. Closing connection.");
                    _client.Disconnect(true);
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _lock.Dispose();
        _idleTimer?.Dispose();
    }
}