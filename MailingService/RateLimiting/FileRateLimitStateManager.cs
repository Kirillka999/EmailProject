namespace MailingService.RateLimiting;

public class FileRateLimitStateManager : IRateLimitStateManager, IDisposable
{
    private const string FilePath = "state/ban_until.txt";
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public FileRateLimitStateManager()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public async Task BanUntilAsync(DateTime expirationTime)
    {
        await _semaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(FilePath, expirationTime.ToString("O"));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<DateTime?> GetBanExpirationAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(FilePath);
            if (DateTime.TryParse(content, out var expirationTime))
            {
                return expirationTime.ToUniversalTime();
            }
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearBanAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}