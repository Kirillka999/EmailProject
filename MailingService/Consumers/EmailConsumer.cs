using MailingService.Models;
using MassTransit;

namespace MailingService.Consumers;

public class EmailConsumer : IConsumer<EmailMessage>
{
    private readonly ILogger<EmailConsumer> _logger;

    public EmailConsumer(ILogger<EmailConsumer> logger)
    {
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<EmailMessage> context)
    {
        var message = context.Message;
        
        _logger.LogInformation($"[RabbitMQ] Отправка письма на {message.ToEmail}.");
        
        await Task.Delay(500); 

        _logger.LogInformation($"[RabbitMQ] Письмо для {message.ToEmail} успешно отправлено!");
    }
}