namespace Shared.Events;

public class NotificationEvent<T> where T : class
{
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public T Data { get; set; } = null!;
}