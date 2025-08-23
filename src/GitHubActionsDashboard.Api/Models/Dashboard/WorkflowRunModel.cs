using Octokit;

namespace GitHubActionsDashboard.Api.Models.Dashboard;

public record WorkflowRunModel
{
    public required long Id { get; init; }
    public required long WorkflowId { get; init; }
    public required string NodeId { get; init; }
    public required StringEnum<WorkflowRunConclusion>? Conclusion { get; init; }
    public required string HeadBranch { get; init; }
    public required string Event { get; init; }
    public required long RunNumber { get; init; }
    public string? TriggeringActor { get; init; }
    public required StringEnum<WorkflowRunStatus> Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string HtmlUrl { get; init; }

    public RagStatus RagStatus
    {
        get
        {
            if (Conclusion == WorkflowRunConclusion.Failure ||
                Conclusion == WorkflowRunConclusion.StartupFailure ||
                Conclusion == WorkflowRunConclusion.TimedOut)
            {
                return RagStatus.Red;
            }

            if (Conclusion == WorkflowRunConclusion.ActionRequired ||
                Conclusion == WorkflowRunConclusion.Cancelled ||
                Conclusion == WorkflowRunConclusion.Skipped)
            {
                return RagStatus.Amber;
            }

            if (Conclusion == WorkflowRunConclusion.Success)
            {
                return RagStatus.Green;
            }

            return RagStatus.None;
        }
    }
}

