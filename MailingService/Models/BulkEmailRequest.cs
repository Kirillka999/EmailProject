namespace MailingService.Models;

public class BulkEmailRequest
{
    public string EmailBodyTemplate { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public List<RecipientDto> Recipients { get; set; } = new();
}