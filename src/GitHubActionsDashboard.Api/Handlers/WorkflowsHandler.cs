using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace GitHubActionsDashboard.Api.Handlers;

public static class WorkflowsHandler
{
    public static async Task<Ok<IEnumerable<Workflow>>> Handle([FromServices]IGitHubClient client, [FromBody] CrossRepositoryRequest request)
    {
        IEnumerable<(string Owner, string Repository)> repos = request.Repositories.SelectMany(or => or.Value.Select(r => (or.Key, r)));

        List<Workflow> workflows = [];
        List<Task<WorkflowsResponse>> listTasks = [];

        foreach (var repo in repos)
        {
            listTasks.Add(client.Actions.Workflows.List(repo.Owner, repo.Repository));
        }

        await Task.WhenAll(listTasks);

        foreach (var task in listTasks)
        {
            workflows.AddRange(task.Result.Workflows);
        }

        return TypedResults.Ok(workflows.AsEnumerable());
    }
}
