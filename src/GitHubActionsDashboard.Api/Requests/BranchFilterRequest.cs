namespace GitHubActionsDashboard.Api.Requests;

public record BranchFilterRequest
{
    public IEnumerable<string> BranchFilters { get; init; } = [];
}
