namespace MailingService.Models;

public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public bool EnableSsl { get; set; }
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public int ConnectionTimeoutSeconds { get; set; }
}