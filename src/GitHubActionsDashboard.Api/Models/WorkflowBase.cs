namespace GitHubActionsDashboard.Api.Models;

public record WorkflowBase
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
}
