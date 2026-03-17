using Shared.Events;

namespace MailingService.Interfaces;

public interface IEmailService
{
    Task SendEmail(EmailNotificationEvent eventData, Guid messageId);
}