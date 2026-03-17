namespace MailingService.Entities;

public enum EmailStatusEnum
{
    Processing = 1,
    Sent = 2,
    RateLimited = 3,
    Failed = 4
}