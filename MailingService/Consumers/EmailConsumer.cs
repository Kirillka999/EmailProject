using System.Text.Json; using MailingService.Database;
using MailingService.Entities;
using MailingService.Exceptions;
using MailingService.Models;
using MailingService.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp; 
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
        
            _logger.LogInformation("[EmailConsumer] Отправка письма на {Email}...", eventData.Email);
            
            await _connectionManager.ExecuteAsync(async client =>
            {
                await client.SendAsync(message);
            });
            
            emailLog.Status = "Sent";
            _logger.LogInformation("[EmailConsumer] Письмо успешно отправлено.");
            
            _mailDbContext.EmailLogs.Add(emailLog);
            await _mailDbContext.SaveChangesAsync(); 
        }
        catch (Exception e) when (IsRateLimitError(e))
        {
            _logger.LogCritical("[EmailConsumer] ЛИМИТ GOOGLE! Письмо вызвало исключение, сработает KillSwitch.");
            
            emailLog.Status = "RateLimited";
            emailLog.ErrorMessage = "Сработал лимит Google: " + e.Message;
            
            _mailDbContext.EmailLogs.Add(emailLog);
            await _mailDbContext.SaveChangesAsync();
            
            throw new RateLimitException("Google SMTP Rate Limit Reached.", e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[EmailConsumer] Ошибка обработки/отправки письма.");
            
            emailLog.Status = "Failed";
            emailLog.ErrorMessage = e.Message;
            
            _mailDbContext.EmailLogs.Add(emailLog);
            await _mailDbContext.SaveChangesAsync();
        }
    }
    
    private bool IsRateLimitError(Exception ex)
    {
        if (ex is SmtpCommandException smtpEx)
        {
            if (smtpEx.StatusCode == SmtpStatusCode.MailboxBusy || 
                smtpEx.StatusCode == SmtpStatusCode.ServiceNotAvailable)
            {
                return true;
            }
        }

        var msg = ex.Message;
        return msg.Contains("429") || 
               msg.Contains("Too Many Requests") || 
               msg.Contains("4.2.1") || 
               msg.Contains("4.7.28") || 
               msg.Contains("rate limit") ||
               msg.Contains("unusually high rate");
    }
}