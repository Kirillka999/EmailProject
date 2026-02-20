namespace MailingService.Exceptions;

public class RateLimitException : Exception
{
    public RateLimitException(string message, Exception innerException) : base(message, innerException) { }
}