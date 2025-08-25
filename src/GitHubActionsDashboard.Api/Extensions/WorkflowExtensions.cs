
namespace GitHubActionsDashboard.Api.Models;

public static class WorkflowExtensions
{
    public static IEnumerable<Dashboard.WorkflowModel> ToDashboardWorkflowModel(this IEnumerable<Octokit.Workflow> workflows)
    {
        foreach (var workflow in workflows)
        {
            yield return new Dashboard.WorkflowModel
            {
                Name = workflow.Name,
                Id = workflow.Id,
                NodeId = workflow.NodeId,
                HtmlUrl = workflow.HtmlUrl,
            };
        }
    }

    public static IEnumerable<WorkflowBase> ToWorkflowBase(this IEnumerable<Octokit.Workflow> workflows)
    {
        foreach (var workflow in workflows)
        {
            yield return new WorkflowBase
            {
                Name = workflow.Name,
                Id = workflow.Id,
                NodeId = workflow.NodeId,
                HtmlUrl = workflow.HtmlUrl,
            };
        }
    }
}
