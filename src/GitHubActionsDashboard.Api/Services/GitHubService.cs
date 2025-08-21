using System.Net;
using System.Text.Json;
using GitHubActionsDashboard.Api.Models.Dashboard;
using Microsoft.Extensions.Caching.Distributed;
using Octokit;

namespace GitHubActionsDashboard.Api.Services;

public interface IGitHubService
{
    Task<IEnumerable<WorkflowModel>> GetWorkflowsAsync(string owner, string repo, CancellationToken cancellationToken);

    Task<WorkflowRunsResponse> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, string? branch, CancellationToken cancellationToken);
}

internal class GitHubService(IGitHubClient gitHubClient, IDistributedCache cache) : IGitHubService
{
    private readonly SemaphoreSlim _gate = new(8);

    public async Task<IEnumerable<WorkflowModel>> GetWorkflowsAsync(string owner, string repo, CancellationToken cancellationToken)
    {
        var cacheKey = $"gh:workflows:{owner}/{repo}";

        var cachedWorkflows = await TryGetFromCache(cacheKey, cancellationToken);
        if (cachedWorkflows != null)
        {
            return cachedWorkflows;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await Jitter(cancellationToken);
            var response = await OctoCall(() => gitHubClient.Actions.Workflows.List(owner, repo), cancellationToken);

            var workflows = response.Workflows.ToWorkflowModel();

            await TryCacheWorkflows(cacheKey, workflows, cancellationToken);

            return workflows;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<WorkflowRunsResponse> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, string? branch, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await Jitter(cancellationToken);
            var req = new WorkflowRunsRequest
            {
                Branch = branch,

            };

            return await OctoCall(
                () => gitHubClient.Actions.Workflows.Runs.ListByWorkflow(owner, repo, workflowId, req,
                               new ApiOptions
                               {
                                   PageCount = 1,
                                   PageSize = 20,
                                   StartPage = 1,
                               }), cancellationToken);
        }
        finally { _gate.Release(); }
    }

    // Fan out safely across many workflows
    public Task<WorkflowRunsResponse[]> GetLastRunsForManyAsync(
        string owner, string repo, IEnumerable<long> workflowIds, int perPage, string? branch, CancellationToken ct)
    {
        var tasks = workflowIds.Select(id => GetLastRunsAsync(owner, repo, id, perPage, branch, ct));
        return Task.WhenAll(tasks);
    }

    private static async Task<T> OctoCall<T>(Func<Task<T>> operation, CancellationToken cancellationToken, int maxAttempts = 4)
    {
        const int baseMs = 250;
        const int capMs = 8000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (AbuseException ex) when (attempt < maxAttempts - 1)
            {
                // Secondary rate-limit; honor Retry-After if present.
                var delay = ex.RetryAfterSeconds.HasValue
                    ? TimeSpan.FromSeconds(Math.Clamp(ex.RetryAfterSeconds.Value, 1, 60))
                    : FullJitter(attempt, baseMs, capMs);
                await Task.Delay(delay, cancellationToken);
                continue;
            }
            catch (RateLimitExceededException ex) when (attempt < maxAttempts - 1)
            {
                // Core rate limit; wait until reset.
                var delay = ex.Reset - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
                if (delay < TimeSpan.Zero) delay = FullJitter(attempt, baseMs, capMs);
                await Task.Delay(delay, cancellationToken);
                continue;
            }
            catch (ApiException ex) when (attempt < maxAttempts - 1 && IsRetryable(ex))
            {
                await Task.Delay(FullJitter(attempt, baseMs, capMs), cancellationToken);
                continue;
            }
            catch (HttpRequestException) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(FullJitter(attempt, baseMs, capMs), cancellationToken);
                continue;
            }
        }

        // Final try (bubble on failure)
        return await operation();

        static bool IsRetryable(ApiException ex)
        {
            var code = (int)ex.StatusCode;
            if (code is 500 or 502 or 503 or 504) return true;
            if (ex.StatusCode == HttpStatusCode.Forbidden) // only retry if server hinted a backoff
                return ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("abuse", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        static TimeSpan FullJitter(int attempt, int baseMs, int capMs)
        {
            var max = Math.Min(capMs, (int)(baseMs * Math.Pow(2, attempt)));
            return TimeSpan.FromMilliseconds(Random.Shared.Next(0, Math.Max(1, max)));
        }
    }

    private static Task Jitter(CancellationToken cancellationToken) =>
        Task.Delay(Random.Shared.Next(0, 200), cancellationToken);

    private async Task<IEnumerable<WorkflowModel>?> TryGetFromCache(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (String.IsNullOrEmpty(cachedJson))
                return null;

            return JsonSerializer.Deserialize<IEnumerable<WorkflowModel>>(cachedJson);
        }
        catch (JsonException)
        {
            // Corrupted cache data, remove it
            await cache.RemoveAsync(cacheKey, cancellationToken);
            return null;
        }
        catch (InvalidOperationException)
        {
            // Redis connection issue, continue without cache
            return null;
        }
    }

    private async Task TryCacheWorkflows(string cacheKey, IEnumerable<WorkflowModel> workflows, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(workflows);
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
               AbsoluteExpirationRelativeToNow =  TimeSpan.FromMinutes(30)
            }, cancellationToken);
        }
        catch (JsonException)
        {
            // Serialization failed, log but don't fail the request
            // TODO: Add logging
        }
        catch (InvalidOperationException)
        {
            // Redis connection issue, continue without caching
            // TODO: Add logging
        }
    }
}
