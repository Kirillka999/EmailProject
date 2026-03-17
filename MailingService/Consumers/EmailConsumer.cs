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
        
        await _emailService.SendEmail(eventData, messageId);
    }
}