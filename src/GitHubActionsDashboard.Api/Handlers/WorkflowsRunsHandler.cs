using GitHubActionsDashboard.Api.Models.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using OwnerRepo = (string Owner, string Repository);

namespace GitHubActionsDashboard.Api.Handlers;

public static class WorkflowRunsHandler
{
    public static async Task<Ok<IEnumerable<RepositoryModel>>> Handle([FromServices] IGitHubClient client, [FromServices] Octokit.GraphQL.Connection graphQlConnection, [FromBody] CrossRepositoryRequest request)
    {
        IEnumerable<OwnerRepo> repos = request.Repositories.SelectMany(or => or.Value.Select(r => (or.Key, r)));

        List<Task<Repository>> repoTasks = [.. repos.Select(repo => client.Repository.Get(repo.Owner, repo.Repository))];

        Dictionary<Repository, List<Workflow>> workflows = [];
        Dictionary<Repository, Task<WorkflowsResponse>> workflowTasks = [];

        List<Task<WorkflowRunsResponse>> runsTasks = [];
        List<WorkflowRun> workflowRuns = [];

        List<RepositoryModel> results = [];

        var repositories = await Task.WhenAll(repoTasks);

        foreach (var repo in repositories)
        {
            workflowTasks.Add(repo, client.Actions.Workflows.List(repo.Owner.Name ?? repo.Owner.Login, repo.Name));
        }

        await Task.WhenAll(workflowTasks.Values);

        foreach (var task in workflowTasks)
        {
            workflows.Add(task.Key, [.. task.Value.Result.Workflows.OrderBy(w => w.Name)]);
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

                //runsTasks.Add(client.Actions.Workflows.Runs.List(repo.Owner.Login, repo.Name, new WorkflowRunsRequest
                runsTasks.Add(client.Actions.Workflows.Runs.ListByWorkflow(repo.Owner.Name ?? repo.Owner.Login, repo.Name, workflow.Id, new WorkflowRunsRequest
                {
                    Branch = branch,
                },
                new ApiOptions
                {
                    PageCount = 1,
                    PageSize = 20,
                    StartPage = 1,
                }));
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
                Details = workflowRepo.Key,
                Workflows = workflowRepo.Value.Select(workflow => new WorkflowModel
                {
                    Details = workflow,
                    Runs = workflowRuns.Where(run => run.WorkflowId == workflow.Id).Select(wr => new WorkflowRunModel { Details = wr })
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
