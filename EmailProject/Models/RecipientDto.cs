namespace EmailProject.Models;

public class RecipientDto
{
    public string Email { get; set; } = string.Empty;
    
    public Dictionary<string, string> Placeholders { get; set; } = new();
}