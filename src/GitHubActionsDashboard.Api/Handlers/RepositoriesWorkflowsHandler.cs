using GitHubActionsDashboard.Api.Models.Dashboard;
using GitHubActionsDashboard.Api.Requests;
using GitHubActionsDashboard.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GitHubActionsDashboard.Api.Handlers;

public static class RepositoriesWorkflowsHandler
{
    public static async Task<Ok<IEnumerable<WorkflowModel>>> Handle([FromServices] IGitHubService gitHubService, [FromRoute] string owner, [FromRoute] string repo, [FromBody] BranchFilterRequest request, CancellationToken cancellationToken)
    {
        Dictionary<WorkflowModel, Task<IEnumerable<WorkflowRunModel>>> runsTasks = [];

        IEnumerable<WorkflowModel> workflows = await gitHubService.GetWorkflowsAsync(owner, repo, cancellationToken);

        foreach (var workflow in workflows)
        {
            runsTasks.Add(workflow, gitHubService.GetLastRunsAsync(owner, repo, workflow.Id, 1, request.BranchFilters, cancellationToken));
        }

        await Task.WhenAll(runsTasks.Values);

        foreach (var runsTask in runsTasks)
        {
            runsTask.Key.Runs.AddRange(runsTask.Value.Result);

        }

        return TypedResults.Ok(workflows.AsEnumerable());
    }
}
