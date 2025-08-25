namespace GitHubActionsDashboard.Api.Models;

public record RepositoryBase
{
    public required string Name { get; init; }

    public required string Owner { get; init; }

    public required string NodeId { get; init; }

    public required string HtmlUrl { get; init; }
}
