using Octokit;

namespace GitHubActionsDashboard.Api.Models.Dashboard;

public record WorkflowModel
{
    /// <summary>
    /// The Id for this workflow.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// GraphQL Node Id.
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// Name of the workflow.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The URL for the HTML view of this workflow.
    /// </summary>
    public required string HtmlUrl { get; init; }

    public IEnumerable<WorkflowRunModel> Runs { get; init; } = [];

    public RagStatus RunStatus { get; init; }

    public RagStatus OverallStatus
    {
        get
        {
            //var conclusions = Runs.Select(r => r.Details.Conclusion).Distinct();
            var conclusions = Runs.GroupBy(r => r.HeadBranch).Select(rg => rg.OrderByDescending(r => r.UpdatedAt).First()).Select(r => r.Conclusion);

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
}
