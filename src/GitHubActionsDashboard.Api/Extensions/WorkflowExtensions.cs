using GitHubActionsDashboard.Api.Models.Dashboard;

namespace Octokit;

public static class WorkflowExtensions
{
    public static IEnumerable<WorkflowModel> ToWorkflowModel(this IEnumerable<Workflow> workflows)
    {
        foreach (var workflow in workflows)
        {
            yield return new WorkflowModel
            {
                Name = workflow.Name,
                Id = workflow.Id,
                NodeId = workflow.NodeId,
                HtmlUrl = workflow.HtmlUrl,
            };
        }
    }
}
