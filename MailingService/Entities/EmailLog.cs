namespace MailingService.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public EmailStatusEnum Status { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}