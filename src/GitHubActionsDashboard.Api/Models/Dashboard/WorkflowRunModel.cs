using Octokit;

namespace GitHubActionsDashboard.Api.Models.Dashboard;

public record WorkflowRunModel
{
    public required WorkflowRun Details { get; init; }

    public RagStatus RagStatus
    {
        get
        {
            if (Details.Conclusion == WorkflowRunConclusion.Failure ||
                Details.Conclusion == WorkflowRunConclusion.StartupFailure ||
                Details.Conclusion == WorkflowRunConclusion.TimedOut)
            {
                return RagStatus.Red;
            }

            if (Details.Conclusion == WorkflowRunConclusion.ActionRequired ||
                Details.Conclusion == WorkflowRunConclusion.Cancelled ||
                Details.Conclusion == WorkflowRunConclusion.Skipped)
            {
                return RagStatus.Amber;
            }

            if (Details.Conclusion == WorkflowRunConclusion.Success)
            {
                return RagStatus.Green;
            }

            return RagStatus.None;
        }
    }
}

