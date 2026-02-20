namespace Shared.Events;

public class NotificationEvent
{
    public string Email { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string TemplateName { get; init; } = string.Empty; 
    public string ModelTypeName { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty; 
}