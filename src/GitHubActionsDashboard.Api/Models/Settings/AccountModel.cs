namespace GitHubActionsDashboard.Api.Models.Settings;

public record AccountModel
{
    public required string Login { get; init; }

    public required string AvatarUrl { get; init; }

    public required string HtmlUrl { get; init; }

    public Octokit.AccountType? Type { get; init; }

    public IList<RepositoryModel> Repositories { get; init; } = [];
}
