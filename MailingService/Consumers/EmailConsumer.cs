using System.Net.Sockets;
using MailingService.Exceptions;
using MailingService.Interfaces;
using MassTransit;
using Shared.Events;

namespace MailingService.Consumers;

public class EmailConsumer : IConsumer<EmailNotificationEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailConsumer> _logger;
    
    public EmailConsumer(IEmailService emailService, ILogger<EmailConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<EmailNotificationEvent> context)
    {
        _logger.LogInformation("[EmailConsumer] Получено сообщение на отправку: {MessageId}", context.MessageId);

        var eventData = context.Message;
        var messageId = context.MessageId ?? Guid.NewGuid();
        
        // =========================================================
        // БЛОК ТЕСТИРОВАНИЯ ОШИБОК (ВРЕМЕННЫЙ КОД) - НАЧАЛО
        // =========================================================
        if (eventData.ModelTypeName.Contains("RateLimitTestTemplate"))
        {
            _logger.LogWarning("[TEST DETECTOR] Обнаружен шаблон RateLimitTest. Симулируем падение с ошибкой 429...");
            throw new RateLimitException("SIMULATED GOOGLE RATE LIMIT 429", new Exception("Test inner exception"));
        }

        if (eventData.ModelTypeName.Contains("SocketErrorTestTemplate"))
        {
            _logger.LogWarning("[TEST DETECTOR] Обнаружен шаблон SocketErrorTest. Симулируем обрыв сети...");
            throw new SocketException((int)SocketError.ConnectionRefused); 
        }
        // =========================================================
        // БЛОК ТЕСТИРОВАНИЯ ОШИБОК (ВРЕМЕННЫЙ КОД) - КОНЕЦ
        // =========================================================
        
        await _emailService.SendEmail(eventData, messageId);
    }
}