namespace MailingService.Models;

public class RabbitMqSettings
{
    public string Host { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;
    public int KillSwitchTimeoutMinutes { get; init; }
}