using System.Diagnostics;
using GitHubActionsDashboard.Api.Models.Dashboard;
using GitHubActionsDashboard.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using OwnerRepo = (string Owner, string Repository);

namespace GitHubActionsDashboard.Api.Handlers;

public static class WorkflowRunsHandler
{
    public static async Task<Ok<IEnumerable<RepositoryModel>>> Handle([FromServices] IGitHubClient client, [FromServices] IGitHubService gitHubService, [FromBody] CrossRepositoryRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<OwnerRepo> repos = request.Repositories.SelectMany(or => or.Value.Select(r => (or.Key, r)));

        List<Task<Repository>> repoTasks = [.. repos.Select(repo => client.Repository.Get(repo.Owner, repo.Repository))];

        Dictionary<Repository, IEnumerable<WorkflowModel>> workflows = [];
        Dictionary<Repository, Task<IEnumerable<WorkflowModel>>> workflowTasks = [];

        List<Task<WorkflowRunsResponse>> runsTasks = [];
        List<WorkflowRun> workflowRuns = [];

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
            foreach (var workflow in workflows[repo])
            {
                string? branch = null;
                if (request.BranchFilters.Count() == 1 && !request.BranchFilters.First().Contains('*'))
                {
                    branch = request.BranchFilters.First();
                }

                runsTasks.Add(gitHubService.GetLastRunsAsync(repo.Owner.Login, repo.Name, workflow.Id, 20, branch, cancellationToken));

                /* //runsTasks.Add(client.Actions.Workflows.Runs.List(repo.Owner.Login, repo.Name, new WorkflowRunsRequest
                 runsTasks.Add(client.Actions.Workflows.Runs.ListByWorkflow(repo.Owner.Name ?? repo.Owner.Login, repo.Name, workflow.Id, new WorkflowRunsRequest
                 {
                     Branch = branch,
                 },
                 new ApiOptions
                 {
                     PageCount = 1,
                     PageSize = 20,
                     StartPage = 1,
                 }));*/
            }
        }

        try
        {
            await Task.WhenAll(runsTasks);
        }
        catch (Octokit.ForbiddenException fex)
        {
            Console.WriteLine($"Forbidden access to workflow runs");
            foreach (var header in fex.HttpResponse.Headers)
            {
                Console.WriteLine($"{header.Key}: {header.Value}");
                Debug.WriteLine($"{header.Key}: {header.Value}");
            }
        }

        foreach (var task in runsTasks)
        {
            workflowRuns.AddRange(task.Result.WorkflowRuns.Where(wr => MatchBranch(wr, request.BranchFilters)));
        }

        foreach (var workflowRepo in workflows)
        {
            results.Add(new RepositoryModel
            {
                Name = workflowRepo.Key.Name,
                Owner = workflowRepo.Key.Owner.Name ?? workflowRepo.Key.Owner.Login,
                NodeId = workflowRepo.Key.NodeId,
                HtmlUrl = workflowRepo.Key.HtmlUrl,
                Workflows = workflowRepo.Value.Select(workflow => new WorkflowModel
                {
                    Name = workflow.Name,
                    Id = workflow.Id,
                    NodeId = workflow.NodeId,
                    HtmlUrl = workflow.HtmlUrl,
                    Runs = workflowRuns.Where(run => run.WorkflowId == workflow.Id).Select(wr => new WorkflowRunModel
                    {
                        Id = wr.Id,
                        NodeId = wr.NodeId,
                        Conclusion = wr.Conclusion,
                        CreatedAt = wr.CreatedAt,
                        Event = wr.Event,
                        HeadBranch = wr.HeadBranch,
                        HtmlUrl = wr.HtmlUrl,
                        RunNumber = wr.RunNumber,
                        Status = wr.Status,
                        TriggeringActor = wr.TriggeringActor?.Name ?? wr.TriggeringActor?.Login,
                        UpdatedAt = wr.UpdatedAt,
                    })
                })
            });
        }

        return TypedResults.Ok(results.AsEnumerable());
    }

    private static bool MatchBranch(WorkflowRun workflowRun, IEnumerable<string> branchFilters)
    {
        if (!branchFilters.Any()) return true;

        if (branchFilters.Contains(workflowRun.HeadBranch)) return true;

        var startsWith = branchFilters.Where(b => b.EndsWith('*')).Select(b => b.Trim('*'));

        return startsWith.Any(workflowRun.HeadBranch.StartsWith);
    }
}
