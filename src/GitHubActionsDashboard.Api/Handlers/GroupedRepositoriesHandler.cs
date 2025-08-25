using GitHubActionsDashboard.Api.Models.Settings;
using GitHubActionsDashboard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GitHubActionsDashboard.Api.Handlers;

public static class GroupedRepositoriesHandler
{
    public static Task<IEnumerable<AccountModel>> Handle([FromServices] ISettingsService settingsService, CancellationToken cancellationToken) =>
        settingsService.ListAllWorkflowsAsync(cancellationToken);
}
