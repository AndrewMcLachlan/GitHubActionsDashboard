using Octokit;

namespace GitHubActionsDashboard.Api.Models.Dashboard;

public record RepositoryModel
{
    public required string Name { get; init; }

    public required string Owner { get; init; }

    public required string NodeId { get; init; }

    public required string HtmlUrl { get; init; }

    public RagStatus OverallStatus
    {
        get
        {
            var conclusions = Workflows.SelectMany(w => w.Runs.GroupBy(r => r.HeadBranch).Select(rg => rg.OrderByDescending(r => r.UpdatedAt).First())).Select(r => r.Conclusion);

            if (conclusions.Contains(WorkflowRunConclusion.Failure) ||
                conclusions.Contains(WorkflowRunConclusion.StartupFailure) ||
                conclusions.Contains(WorkflowRunConclusion.TimedOut))
            {
                return RagStatus.Red;
            }

            if (conclusions.Contains(WorkflowRunConclusion.ActionRequired) ||
                conclusions.Contains(WorkflowRunConclusion.Cancelled) ||
                conclusions.Contains(WorkflowRunConclusion.Skipped))
            {
                return RagStatus.Amber;
            }

            if (conclusions.Contains(WorkflowRunConclusion.Success))
            {
                return RagStatus.Green;
            }

            return RagStatus.None;
        }
    }

    public IEnumerable<WorkflowModel> Workflows { get; init; } = [];
}
