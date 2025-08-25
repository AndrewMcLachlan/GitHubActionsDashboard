namespace GitHubActionsDashboard.Api.Models.Settings;

public record RepositoryModel : RepositoryBase
{
    public required string FullName { get; init; }

    public IList<WorkflowBase> Workflows { get; init; } = [];
}
