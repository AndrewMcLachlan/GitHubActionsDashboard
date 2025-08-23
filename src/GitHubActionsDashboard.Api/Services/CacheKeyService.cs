using System.Security.Cryptography;
using System.Text;
using GitHubActionsDashboard.Api.Exceptions;

namespace GitHubActionsDashboard.Api.Services;

public interface ICacheKeyService
{
    string GetCacheKey(string key);
}

public class CacheKeyService(IHttpContextAccessor httpContextAccessor) : ICacheKeyService
{
    public string GetCacheKey(string key)
    {
        var httpContext = httpContextAccessor.HttpContext;
        // TODO: base cache keys on user logins or IDs so they can span sessions
        var userToken = httpContext?.Session.GetString("github_access_token");
        if (String.IsNullOrEmpty(userToken))
        {
            throw new UnauthorizedException();
        }

        // Use token hash for cache key to isolate users
        var userHash = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(userToken)));

        return $"{userHash}:{key}";
    }
}
