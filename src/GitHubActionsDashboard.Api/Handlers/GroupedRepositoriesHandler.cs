using GitHubActionsDashboard.Api.Models.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GitHubActionsDashboard.Api.Handlers;

public static class GroupedRepositoriesHandler
{
    public static async Task<Ok<IEnumerable<Account>>> Handle([FromServices] Octokit.IGitHubClient client)
    {
        List<Account> groupedRepositories = [];

        var user = await client.User.Current();

        var repos = await client.Repository.GetAllForCurrent();

        repos.GroupBy(r => r.Owner, new OwnerEqualityComparer())
            .ToList()
            .ForEach(g =>
            {
                groupedRepositories.Add(new Account()
                {
                    Login = g.Key.Login,
                    Type = g.Key.Type,
                    AvatarUrl = g.Key.AvatarUrl,
                    HtmlUrl = g.Key.HtmlUrl,
                    Repositories = g.Select(r => new Repository
                    {
                        Name = r.Name,
                        FullName = r.FullName,
                        Owner = r.Owner.Login,
                        HtmlUrl = r.HtmlUrl,
                    }),
                });
            });

        return TypedResults.Ok(groupedRepositories.AsEnumerable());
    }
}
