namespace MailingService.Models;

public record EmailMessage
{
    public string ToEmail { get; init; }
    public string Body { get; init; }
}