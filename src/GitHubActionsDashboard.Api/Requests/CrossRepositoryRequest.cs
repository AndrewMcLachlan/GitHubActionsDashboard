namespace GitHubActionsDashboard.Api.Requests;

public record CrossRepositoryRequest
{
    public Dictionary<string, List<string>> Repositories { get; init;  } = [];
}
