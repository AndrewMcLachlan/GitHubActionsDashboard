using Octokit;

namespace GitHubActionsDashboard.Api.Models.Dashboard;

public record WorkflowModel : Models.WorkflowBase
{
    public IList<WorkflowRunModel> Runs { get; init; } = [];

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
