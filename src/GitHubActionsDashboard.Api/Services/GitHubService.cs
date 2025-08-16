/*using GitHubActionsDashboard.Api.Exceptions;
using Octokit;

namespace GitHubActionsDashboard.Api.Services;

public interface IGitHubService
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