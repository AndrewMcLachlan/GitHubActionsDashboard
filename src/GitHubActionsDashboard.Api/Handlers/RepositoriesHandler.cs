using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace GitHubActionsDashboard.Api.Handlers;

public static class RepositoriesHandler
{
    public static async Task<Ok<IEnumerable<Repository>>> Handle([FromServices]IGitHubClient client)
    {
        List<Repository> repositories = [];

        var orgs = await client.Organization.GetAllForCurrent();

        foreach (var org in orgs)
        {
            repositories.AddRange(await client.Repository.GetAllForOrg(org.Name));
        }

        repositories.AddRange(await client.Repository.GetAllForCurrent());

        return TypedResults.Ok(repositories.AsEnumerable());
    }
}
