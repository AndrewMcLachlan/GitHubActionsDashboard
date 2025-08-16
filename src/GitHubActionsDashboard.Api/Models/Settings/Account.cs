namespace GitHubActionsDashboard.Api.Models.Settings;

public record Account
{
    public required string Login { get; init; }

    public required string AvatarUrl { get; init; }

    public required string HtmlUrl { get; init; }

    public Octokit.AccountType? Type { get; init; }

    public IEnumerable<Repository> Repositories { get; init; } = [];
}
