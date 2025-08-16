namespace GitHubActionsDashboard.Api.Handlers;

public record CrossRepositoryRequest
{
    public Dictionary<string, List<string>> Repositories { get; init;  } = [];

    public IEnumerable<string> BranchFilters { get; init; } = [];
}
