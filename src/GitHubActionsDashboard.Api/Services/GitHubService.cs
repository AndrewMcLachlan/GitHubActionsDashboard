using System.Net;
using Octokit;

namespace GitHubActionsDashboard.Api.Services;

public interface IGitHubService
{
    Task<WorkflowRunsResponse> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, string? branch, CancellationToken cancellationToken);
}

internal class GitHubService(IGitHubClient gitHubClient) : IGitHubService
{
    private readonly SemaphoreSlim _gate = new(8);

    public async Task<WorkflowRunsResponse> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, string? branch, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            // tiny pre-call jitter helps avoid burst patterns
            await Task.Delay(Random.Shared.Next(0, 200), cancellationToken);

            var req = new WorkflowRunsRequest
            {
                //                Status = CheckRun
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

    private static async Task<T> OctoCall<T>(Func<Task<T>> op, CancellationToken ct, int maxAttempts = 4)
    {
        const int baseMs = 250;
        const int capMs = 8000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await op();
            }
            catch (AbuseException ex) when (attempt < maxAttempts - 1)
            {
                // Secondary rate-limit; honor Retry-After if present.
                var delay = ex.RetryAfterSeconds.HasValue
                    ? TimeSpan.FromSeconds(Math.Clamp(ex.RetryAfterSeconds.Value, 1, 60))
                    : FullJitter(attempt, baseMs, capMs);
                await Task.Delay(delay, ct);
                continue;
            }
            catch (RateLimitExceededException ex) when (attempt < maxAttempts - 1)
            {
                // Core rate limit; wait until reset.
                var delay = ex.Reset - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
                if (delay < TimeSpan.Zero) delay = FullJitter(attempt, baseMs, capMs);
                await Task.Delay(delay, ct);
                continue;
            }
            catch (ApiException ex) when (attempt < maxAttempts - 1 && IsRetryable(ex))
            {
                await Task.Delay(FullJitter(attempt, baseMs, capMs), ct);
                continue;
            }
            catch (HttpRequestException) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(FullJitter(attempt, baseMs, capMs), ct);
                continue;
            }
        }

        // Final try (bubble on failure)
        return await op();

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
}


/*public interface IGitHubService
{
    Task<IReadOnlyList<Repository>> GetRepositories(CancellationToken cancellationToken = default);

    Task TestAsync(CancellationToken cancellationToken = default);
}

internal class GitHubService(IGitHubClient gitHubClient) : IGitHubService
{
    public Task<IReadOnlyList<Repository>> GetRepositories(CancellationToken cancellationToken = default)
    {
        try
        {
            Connection connection = new Connection(new ProductHeaderValue("GitHubActionsDashboard", "0.1"))
            {
                Credentials = new Credentials(client.DefaultRequestHeaders.Authorization?.Parameter ?? string.Empty)
            };

            ApiConnection apiConnection = new ApiConnection(connection);
            RepositoriesClient repositoriesClient = new(apiConnection);
            return repositoriesClient.GetAllForCurrent();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IReadOnlyList<Workflow>> GetWorkflows(string owner, string repository, CancellationToken cancellationToken = default)
    {
        WorkflowsResponse response = await gitHubClient.Actions.Workflows.List(owner, repository);

        return response.Workflows;
    }

    public Task TestAsync(CancellationToken cancellationToken = default)
    {
        //EnsureAuthorized();

        return Task.CompletedTask;
    }

    private void EnsureAuthorized()
    {
        var token = client.DefaultRequestHeaders.Authorization;

        if (token?.Parameter is null) throw new UnauthorizedException();
    }
}
*/
