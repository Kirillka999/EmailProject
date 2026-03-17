namespace MailingService.Entities;

public class SmtpSettings
{
    public string Host { get; init; }
    public int Port { get; init; }
    public string Login { get; init; }
    public string Password { get; init; }
    public bool EnableSsl { get; init; }
    public string SenderName { get; init; }
    public string SenderEmail { get; init; }
    public int ConnectionTimeoutSeconds { get; init; }
}