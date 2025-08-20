using System.Text.Json.Serialization;

namespace GitHubActionsDashboard.Api.GraphQL;

internal record QueryBody
{
    [JsonPropertyName("query")]
    public required string Query { get; init; }
}
