namespace GitHubActionsDashboard.Api.Requests;

public record WorkflowsRequest
{
    public record RepositoryWorkflowRequest
    {
        public required string Owner { get; init; }

        public required string Name { get; init; }

        public IReadOnlyList<long> Workflows { get; init; } = [];
    }

    public IReadOnlyList<RepositoryWorkflowRequest> Repositories { get; init; } = [];
}
