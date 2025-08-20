using GitHubActionsDashboard.Api.Models;
using GitHubActionsDashboard.Api.Models.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using Octokit.GraphQL;
using Octokit.GraphQL.Core;
using CheckConclusionState = Octokit.GraphQL.Model.CheckConclusionState;
using OwnerRepo = (string Owner, string Repository);

namespace GitHubActionsDashboard.Api.Handlers;

public static class WorkflowsHandler
{
    public static async Task<Ok<IEnumerable<RepositoryModel>>> Handle([FromServices] IGitHubClient client, [FromServices] Octokit.GraphQL.Connection connection, [FromBody] CrossRepositoryRequest request)
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

        const string gql = @"query($ids:[ID!]!, $per:Int!){
  nodes(ids:$ids){
            ... on Workflow {
                id name
              runs(first:$per){
                    nodes{ databaseId conclusion status headBranch createdAt }
                }
            }
        }
    }";

        Arg<int> per = 20;

        foreach (var repo in workflows.Keys)
        {
            var ids = workflows[repo].Select(w => new ID(w.NodeId)).ToList();

            // nodes(ids:[ID!]!) { ... on Workflow { runs(first: $per) { nodes { ... } } } }
            var query =
            new Query()
            .Nodes(ids)
            .Select(n => n.Cast<Octokit.GraphQL.Core.QueryableValue<Octokit.GraphQL.Model.Workflow>>().Select(wf => new WorkflowWithRuns
            {
                Id = wf.Id,
                Name = wf.Name,
                Runs = wf.Runs(per, null, null, null, null).Nodes.Select(r => new RunDto
                {
                    DatabaseId = r.DatabaseId,
                    RunNumber = r.RunNumber,
                    Status = r.CheckSuite.Status,       // enum
                    Conclusion = r.CheckSuite.Conclusion,   // enum
                    HeadBranch = r.CheckSuite.Branch.Name,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Url = r.Url
                }).ToList()
            }))
            .Compile();

            var result = await connection.Run(query);

            foreach (var workflowRepo in workflows)
            {
                var runs = result
                    .Where(wr => wr.Single().Id.ToString() == workflowRepo.Key.NodeId)
                    .SelectMany(wr => wr.Single().Runs)
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
    public List<RunDto> Runs { get; init; } = new();
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
