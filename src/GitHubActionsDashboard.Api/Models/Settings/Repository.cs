namespace GitHubActionsDashboard.Api.Models.Settings;

public record Repository
{
    public required string Owner { get; init; }

    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string HtmlUrl { get; init; }
}
