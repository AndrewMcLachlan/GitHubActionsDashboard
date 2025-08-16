using GitHubActionsDashboard.Api.Models.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GitHubActionsDashboard.Api.Handlers;

public static class GroupedRepositoriesHandler
{
    public static async Task<Ok<IEnumerable<Account>>> Handle([FromServices]Octokit.IGitHubClient client)
    {
        List<Account> groupedRepositories = [];

        var orgs = await client.Organization.GetAllForCurrent();

        foreach (var org in orgs)
        {
            var repositories = await client.Repository.GetAllForOrg(org.Name);

            groupedRepositories.Add(new Account()
            {
                Login = org.Login,
                Type = org.Type,
                AvatarUrl = org.AvatarUrl,
                HtmlUrl = org.HtmlUrl,
                Repositories = repositories.Select(r => new Repository
                {
                    Name = r.Name,
                    FullName = r.FullName,
                    Owner = r.Owner.Login,
                    HtmlUrl = r.HtmlUrl,
                })
            });
        }

        var user = await client.User.Current();

        groupedRepositories.Add(new Account()
        {
            Login = user.Login,
            Type = user.Type,
            AvatarUrl = user.AvatarUrl,
            HtmlUrl = user.HtmlUrl,
            Repositories = (await client.Repository.GetAllForCurrent()).Select(r => new Repository
            {
                Name = r.Name,
                FullName = r.FullName,
                Owner = r.Owner.Login,
                HtmlUrl = r.HtmlUrl,
            }),
        });

        return TypedResults.Ok(groupedRepositories.AsEnumerable());
    }
}
