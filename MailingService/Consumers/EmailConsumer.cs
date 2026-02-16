using MailingService.Models;
using MailingService.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Events;
using Shared.Templates;

namespace MailingService.Consumers;

public class EmailConsumer<T> : IConsumer<NotificationEvent<T>> where T : class, IEmailTemplate
{
    private readonly ILogger<EmailConsumer<T>> _logger;
    private readonly TemplateRenderer _renderer;
    private readonly SmtpSettings _smtpSettings;
    private readonly SmtpConnectionManager _connectionManager;
    
    public EmailConsumer(ILogger<EmailConsumer<T>> logger, TemplateRenderer renderer,
        IOptions<SmtpSettings> smtpSettings, SmtpConnectionManager connectionManager)
    {
        _logger = logger;
        _renderer = renderer;
        _connectionManager = connectionManager;
        _smtpSettings = smtpSettings.Value;
    }
    
    public async Task Consume(ConsumeContext<NotificationEvent<T>> context)
    {
        var eventData = context.Message;
        string htmlBody = await _renderer.RenderAsync(eventData.Data);
        
        var message = new MimeMessage();
        
        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
        message.To.Add(new MailboxAddress("", eventData.Email));
        
        message.Subject = eventData.Subject;
        message.Body = new TextPart("html")
        {
            Text = htmlBody
        };
        
        _logger.LogInformation("[EmailConsumer] Sending email...");
        await _connectionManager.ExecuteAsync(async client =>
        {
            await client.SendAsync(message);
        });
    }
}