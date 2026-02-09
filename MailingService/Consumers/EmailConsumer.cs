using MailingService.Models;
using MailingService.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MailingService.Consumers;

public class EmailConsumer : IConsumer<EmailMessage>
{
    private readonly ILogger<EmailConsumer> _logger;
    private readonly SmtpSettings _smtpSettings;
    private readonly SmtpConnectionManager _connectionManager;
    
    public EmailConsumer(ILogger<EmailConsumer> logger, IOptions<SmtpSettings> smtpSettings,
        SmtpConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _smtpSettings = smtpSettings.Value;
    }
    
    public async Task Consume(ConsumeContext<EmailMessage> context)
    {
        var message = new MimeMessage();
        
        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
        message.To.Add(new MailboxAddress("", context.Message.ToEmail));
        
        message.Subject = "Рассылка";
        message.Body = new TextPart("html")
        {
            Text = context.Message.Body
        };
        
        _logger.LogInformation("[EmailConsumer] Sending email...");
        await _connectionManager.ExecuteAsync(async client =>
        {
            await client.SendAsync(message);
        });
    }
}