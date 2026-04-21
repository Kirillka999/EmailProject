using System.Text.Json;
using MailingService.Database;
using MailingService.Entities;
using MailingService.Exceptions;
using MailingService.Interfaces;
using MailingService.Razor;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Shared.Events;

namespace MailingService.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly TemplateRenderer _renderer;
    private readonly SmtpSettings _smtpSettings;
    private readonly SmtpConnectionManager _connectionManager;
    private readonly MailingDbContext _dbContext;

    public EmailService(ILogger<EmailService> logger, 
        TemplateRenderer renderer,
        IOptions<SmtpSettings> smtpSettings, 
        SmtpConnectionManager connectionManager, 
        MailingDbContext dbContext)
    {
        _logger = logger;
        _renderer = renderer;
        _smtpSettings = smtpSettings.Value;
        _connectionManager = connectionManager;
        _dbContext = dbContext;
    }
    
    public async Task SendEmail(EmailNotificationEvent eventData, Guid messageId)
    {
        var emailLog = await _dbContext.EmailLogs.FindAsync(messageId);
        
        if (emailLog is not null)
        {
            if (emailLog.Status == EmailStatusEnum.Sent)
            {
                _logger.LogInformation("Письмо {MessageId} уже было успешно отправлено ранее. Пропускаем дубликат.", messageId);
                return;
            }
            
            emailLog.Status = EmailStatusEnum.Processing;
        }
        else
        {
            emailLog = new EmailLog
            {
                Id = messageId,
                Recipient = eventData.Email,
                Subject = "",
                CreatedAt = DateTime.UtcNow,
                Status = EmailStatusEnum.Processing
            };
            _dbContext.EmailLogs.Add(emailLog);
        }
        
        await _dbContext.SaveChangesAsync();
        
        try
        {
            Type modelType = Type.GetType(eventData.ModelTypeName)!;
            object templateModel = JsonSerializer.Deserialize(eventData.Payload, modelType)!;
            
            var (htmlBody, templateSubject) = await _renderer.RenderAsync(modelType, templateModel);
            
            if (string.IsNullOrWhiteSpace(templateSubject))
            {
                throw new InvalidOperationException($"Тема письма не задана! Убедитесь, что в файле .cshtml для {modelType.Name} указано: ViewBag.Subject = \"...\";");
            }
            
            emailLog.Subject = templateSubject;
            emailLog.Body = htmlBody;
            
            var message = CreateMimeMessage(eventData, htmlBody, templateSubject);
        
            _logger.LogInformation("Отправка письма на {Email} с темой '{Subject}'...", eventData.Email, templateSubject);
            
            await _connectionManager.ExecuteAsync(async client =>
            {
                await client.SendAsync(message);
            });
            
            emailLog.Status = EmailStatusEnum.Sent;
            _logger.LogInformation("Письмо успешно отправлено.");
            
            await _dbContext.SaveChangesAsync(); 
        }
        catch (Exception e) when (IsRateLimitError(e))
        {
            _logger.LogCritical("ЛИМИТ GOOGLE! Письмо вызвало исключение, сработает KillSwitch.");
            
            emailLog.Status = EmailStatusEnum.RateLimited;
            AppendError(emailLog, "Сработал лимит Google: " + e.Message);
            
            await _dbContext.SaveChangesAsync();
            
            throw new RateLimitException("Google SMTP Rate Limit Reached.", e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка обработки/отправки письма.");
            
            emailLog.Status = EmailStatusEnum.Failed;
            AppendError(emailLog, e.Message);
            
            await _dbContext.SaveChangesAsync();
            throw;
        }
    }
    
    private MimeMessage CreateMimeMessage(EmailNotificationEvent eventData, string htmlBody, string subject)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
        message.To.Add(new MailboxAddress("", eventData.Email));
        
        message.Subject = subject;
        
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlBody
        };
        return message;
    }

    private void AppendError(EmailLog log, string newErrorMessage)
    {
        var formattedError = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] {newErrorMessage}";

        if (!string.IsNullOrWhiteSpace(log.ErrorMessage))
        {
            log.ErrorMessage += Environment.NewLine;
        }

        log.ErrorMessage += formattedError;
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