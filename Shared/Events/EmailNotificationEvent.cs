namespace Shared.Events;

public class EmailNotificationEvent
{
    public string Email { get; init; } = string.Empty;
    public string ModelTypeName { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    
    public EmailNotificationEvent() { }
    
    internal EmailNotificationEvent(string email, string modelTypeName, string payload)
    {
        Email = email;
        ModelTypeName = modelTypeName;
        Payload = payload;
    }
}