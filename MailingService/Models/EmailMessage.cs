namespace MailingService.Models;

public record EmailMessage
{
    public string Recipient { get; init; }
    public string Body { get; init; }
    public string Subject { get; init; }
}