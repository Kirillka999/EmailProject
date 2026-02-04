namespace EmailProject.Models;

public class BulkEmailRequest
{
    public string EmailBodyTemplate { get; set; } = string.Empty;
    
    public List<RecipientDto> Recipients { get; set; } = new();
}