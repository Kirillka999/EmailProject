using System.Globalization;
using System.Text;
using MailingService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MailingService.BackgroundServices;

public class ErrorQueueReprocessorService : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<ErrorQueueReprocessorService> _logger;
    
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    public ErrorQueueReprocessorService(IOptions<RabbitMqSettings> settings, ILogger<ErrorQueueReprocessorService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"[Reprocessor] Фоновый сервис запущен. Проверка очереди");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollingInterval, stoppingToken);

            try
            {
                await ReprocessErrorQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Reprocessor] Ошибка при попытке перенести письма из _error.");
            }
        }
    }

    private async Task ReprocessErrorQueueAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        var errorQueueName = $"{_settings.QueueName}_error";
        var targetQueueName = _settings.QueueName;
        
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        uint messageCount;
        try
        {
            var queueInfo = await channel.QueueDeclarePassiveAsync(errorQueueName, cancellationToken);
            messageCount = queueInfo.MessageCount;
        }
        catch
        {
            return; 
        }

        if (messageCount == 0)
        {
            return;
        }
        
        
        var requiredDelay = TimeSpan.FromMinutes(_settings.KillSwitchTimeoutMinutes);
        uint movedCount = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(errorQueueName, autoAck: false, cancellationToken);
            if (result == null)
            {
                break;
            }
            
            var faultTime = GetFaultTimestamp(result.BasicProperties);
            
            if (faultTime.HasValue && (DateTime.UtcNow - faultTime.Value) < requiredDelay)
            {
                await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                break; 
            }
            
            var newProps = new BasicProperties(result.BasicProperties);

            await channel.BasicPublishAsync(
                exchange: "", 
                routingKey: targetQueueName,
                mandatory: false,
                basicProperties: newProps, 
                body: result.Body,
                cancellationToken: cancellationToken);
            
            await channel.BasicAckAsync(result.DeliveryTag, multiple: false, cancellationToken);
            movedCount++;
        }

        if (movedCount > 0)
        {
            _logger.LogInformation($"[Reprocessor] Успешно возвращено {movedCount} писем в '{targetQueueName}'.");
        }
    }
    
    private DateTime? GetFaultTimestamp(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers != null && properties.Headers.TryGetValue("MT-Fault-Timestamp", out var value))
        {
            if (value is byte[] bytes)
            {
                var str = Encoding.UTF8.GetString(bytes);
                if (DateTime.TryParse(str, null, DateTimeStyles.RoundtripKind, out var dt))
                {
                    return dt.ToUniversalTime();
                }
            }
        }
        return null;
    }
}