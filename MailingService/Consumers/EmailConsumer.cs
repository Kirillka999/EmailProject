using System.Text.Json;
using MailingService.Data;
using MailingService.Entities;
using MailingService.Models;
using MailingService.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Events;

namespace MailingService.Consumers;

public class EmailConsumer : IConsumer<NotificationEvent>
{
    private readonly ILogger<EmailConsumer> _logger;
    private readonly TemplateRenderer _renderer;
    private readonly SmtpSettings _smtpSettings;
    private readonly SmtpConnectionManager _connectionManager;
    private readonly MailingDbContext _mailDbContext;
    
    public EmailConsumer(ILogger<EmailConsumer> logger, TemplateRenderer renderer,
        IOptions<SmtpSettings> smtpSettings, SmtpConnectionManager connectionManager, MailingDbContext mailDbContext)
    {
        _logger = logger;
        _renderer = renderer;
        _connectionManager = connectionManager;
        _mailDbContext = mailDbContext;
        _smtpSettings = smtpSettings.Value;
    }
    // Polly library для обработки 429 от гугла, либо можно консьюмера затормозить
    // basic cancel и basic nack посмотреть
    // circuit breaker паттерн
    public async Task Consume(ConsumeContext<NotificationEvent> context)
    {
        var eventData = context.Message;
        
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            Recipient = eventData.Email,
            Subject = eventData.Subject,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            Type? modelType = Type.GetType(eventData.ModelTypeName);
            if (modelType == null)
            {
                throw new InvalidOperationException($"Не удалось найти тип модели: {eventData.ModelTypeName}");
            }
            
            object? templateModel = JsonSerializer.Deserialize(eventData.Payload, modelType);
            if (templateModel == null)
            {
                throw new InvalidOperationException("Не удалось десериализовать Payload письма.");
            }
            
            string htmlBody = await _renderer.RenderAsync(eventData.TemplateName, templateModel);
            
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
            
            emailLog.Status = "Sent";
            _logger.LogInformation("[EmailConsumer] Email sent successfully.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[EmailConsumer] Failed to process email.");
            emailLog.Status = "Failed";
            emailLog.ErrorMessage = e.Message;
        }
        
        _mailDbContext.EmailLogs.Add(emailLog);
        await _mailDbContext.SaveChangesAsync(); 
    }
}