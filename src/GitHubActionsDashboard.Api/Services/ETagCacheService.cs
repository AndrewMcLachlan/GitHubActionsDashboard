using Microsoft.Extensions.Caching.Distributed;

namespace GitHubActionsDashboard.Api.Services;

public interface IETagCacheService
{
    Task<string?> GetETagAsync(Uri url, CancellationToken cancellationToken = default);
    Task SetETagAsync(Uri url, string etag, CancellationToken cancellationToken = default);
}

public class ETagCacheService(IDistributedCache cache, ICacheKeyService cacheKeyService) : IETagCacheService
{
    public async Task<string?> GetETagAsync(Uri url, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = cacheKeyService.GetCacheKey($"etag:{url}");
            return await cache.GetStringAsync(cacheKey, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetETagAsync(Uri url, string etag, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = cacheKeyService.GetCacheKey($"etag:{url}");
            await cache.SetStringAsync(cacheKey, etag, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(1) // ETags are valid until content changes
            }, cancellationToken);
        }
        catch
        {
            // Ignore cache failures
        }
    }
}
