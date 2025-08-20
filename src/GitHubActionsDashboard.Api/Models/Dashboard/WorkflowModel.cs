using Octokit;

namespace GitHubActionsDashboard.Api.Models.Dashboard;

public record WorkflowModel
{
    /// <summary>
    /// The Id for this workflow.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// GraphQL Node Id.
    /// </summary>
    public string NodeId { get; private set; }

    /// <summary>
    /// Name of the workflow.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The URL for the HTML view of this workflow.
    /// </summary>
    public string HtmlUrl { get; private set; }

    public required Workflow Details { get; init; }

    public IEnumerable<WorkflowRunModel> Runs { get; init; } = [];

    public RagStatus RunStatus { get; init; }

    public RagStatus OverallStatus
    {
        get
        {
            //var conclusions = Runs.Select(r => r.Details.Conclusion).Distinct();
            var conclusions = Runs.GroupBy(r => r.Details.HeadBranch).Select(rg => rg.OrderByDescending(r => r.Details.UpdatedAt).First()).Select(r => r.Details.Conclusion);

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
