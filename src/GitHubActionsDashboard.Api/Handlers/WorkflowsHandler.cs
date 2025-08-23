using GitHubActionsDashboard.Api.Models.Dashboard;
using GitHubActionsDashboard.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using OwnerRepo = (string Owner, string Repository);

namespace GitHubActionsDashboard.Api.Handlers;

public static class WorkflowsHandler
{
    public static async Task<Ok<IEnumerable<RepositoryModel>>> Handle([FromServices] IGitHubClient client, [FromServices] IGitHubService gitHubService, [FromBody] CrossRepositoryRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<OwnerRepo> repos = request.Repositories.SelectMany(or => or.Value.Select(r => (or.Key, r)));

        List<Task<Repository>> repoTasks = [.. repos.Select(repo => client.Repository.Get(repo.Owner, repo.Repository))];

        Dictionary<Repository, IEnumerable<WorkflowModel>> workflows = [];
        Dictionary<Repository, Task<IEnumerable<WorkflowModel>>> workflowTasks = [];

        List<Task<IEnumerable<WorkflowRunModel>>> runsTasks = [];
        List<WorkflowRunModel> workflowRuns = [];

        List<RepositoryModel> results = [];

        var repositories = await Task.WhenAll(repoTasks);

        foreach (var repo in repositories)
        {
            workflowTasks.Add(repo, gitHubService.GetWorkflowsAsync(repo.Owner.Name ?? repo.Owner.Login, repo.Name, cancellationToken));
        }

        await Task.WhenAll(workflowTasks.Values);

        foreach (var task in workflowTasks)
        {
            workflows.Add(task.Key, [.. task.Value.Result.OrderBy(w => w.Name)]);
        }

        foreach (var repo in repositories)
        {
            //foreach (var workflow in repo.Workworkflows[repo].Select(w => w.Id))
            foreach (var workflow in workflows[repo])
            {
                runsTasks.Add(gitHubService.GetLastRunsAsync(repo.Owner.Login, repo.Name, workflow.Id, 1, repo.DefaultBranch, cancellationToken));
            }
        }

        await Task.WhenAll(runsTasks);

        foreach (var runTask in runsTasks)
        {
            workflowRuns.AddRange(runTask.Result);
        }

        foreach (var workflowRepo in workflows)
        {
            results.Add(new RepositoryModel
            {
                Name = workflowRepo.Key.Name,
                Owner = workflowRepo.Key.Owner.Name ?? workflowRepo.Key.Owner.Login,
                NodeId = workflowRepo.Key.NodeId,
                HtmlUrl = workflowRepo.Key.HtmlUrl,
                Workflows = workflowRepo.Value.Select(workflow => workflow with { Runs = [.. workflowRuns.Where(run => run.WorkflowId == workflow.Id)] }),
            });
        }

        return TypedResults.Ok(results.AsEnumerable());
    }
}
