using GitHubActionsDashboard.Api.Models;
using GitHubActionsDashboard.Api.Models.Dashboard;
using GitHubActionsDashboard.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using Octokit.GraphQL;
using CheckConclusionState = Octokit.GraphQL.Model.CheckConclusionState;
using OwnerRepo = (string Owner, string Repository);

namespace GitHubActionsDashboard.Api.Handlers;

public static class WorkflowsHandler
{
    public static async Task<Ok<IEnumerable<RepositoryModel>>> Handle([FromServices] IGitHubClient client, [FromServices] IGraphQLService graphQLService, [FromBody] CrossRepositoryRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<OwnerRepo> repos = request.Repositories.SelectMany(or => or.Value.Select(r => (or.Key, r)));

        List<Task<Repository>> repoTasks = [.. repos.Select(repo => client.Repository.Get(repo.Owner, repo.Repository))];

        Dictionary<Repository, List<Workflow>> workflows = [];
        Dictionary<Repository, Task<WorkflowsResponse>> workflowTasks = [];

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

        foreach (var repo in workflows.Keys)
        {
            var ids = workflows[repo].Select(w => w.NodeId);

            var result = await graphQLService.GetWorkflowRuns(ids, cancellationToken);

            foreach (var workflowRepo in workflows)
            {
                var runs = result
                    .Where(wr => wr.Id.ToString() == workflowRepo.Key.NodeId)
                    .SelectMany(wr => wr.Runs)
                    .ToList();

                var conclusions = runs.GroupBy(r => r.HeadBranch).Select(rg => rg.OrderByDescending(r => r.UpdatedAt).First()).Select(r => r.Conclusion);

                var status = RagStatus.None;
                if (conclusions.Contains(CheckConclusionState.Failure) ||
                    conclusions.Contains(CheckConclusionState.StartupFailure) ||
                    conclusions.Contains(CheckConclusionState.TimedOut))
                {
                    status = RagStatus.Red;
                }

                if (conclusions.Contains(CheckConclusionState.ActionRequired) ||
                    conclusions.Contains(CheckConclusionState.Cancelled) ||
                    conclusions.Contains(CheckConclusionState.Skipped))
                {
                    status = RagStatus.Amber;
                }

                if (conclusions.Contains(CheckConclusionState.Success))
                {
                    status = RagStatus.Green;
                }

                results.Add(new RepositoryModel
                {
                    Details = workflowRepo.Key,
                    Workflows = workflowRepo.Value.Select(workflow => new WorkflowModel
                    {
                        Details = workflow,
                        RunStatus = status,
                    })
                });
            }
        }

        return TypedResults.Ok(results.AsEnumerable());
    }
}

public record WorkflowWithRuns
{
    public ID Id { get; init; }
    public string Name { get; init; } = "";
    public List<RunDto> Runs { get; init; } = [];
}

public record RunDto
{
    public long? DatabaseId { get; init; }
    public int RunNumber { get; init; }
    public Octokit.GraphQL.Model.CheckStatusState Status { get; init; }
    public CheckConclusionState? Conclusion { get; init; }
    public string? HeadBranch { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string Url { get; init; } = "";
}
