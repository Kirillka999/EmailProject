namespace MailingService.RateLimiting;

public interface IRateLimitStateManager
{
    public Task BanUntilAsync(DateTime expirationTime);
    public Task<DateTime?> GetBanExpirationAsync();
    public Task ClearBanAsync();
}