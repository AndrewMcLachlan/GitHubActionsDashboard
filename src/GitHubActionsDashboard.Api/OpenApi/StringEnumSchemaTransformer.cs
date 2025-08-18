using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Octokit;

namespace GitHubActionsDashboard.Api.OpenApi;

public class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (IsStringEnumSchema(context))
        {
            schema.Type = "string";
            schema.Properties?.Clear();
            schema.AllOf?.Clear();
            schema.OneOf?.Clear();
            schema.AnyOf?.Clear();
            schema.Items = null;
            schema.AdditionalProperties = null;
        }
        return Task.CompletedTask;
    }

    private static bool IsStringEnumSchema(OpenApiSchemaTransformerContext context)
    {
        var type = context.JsonTypeInfo.Type;

        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(StringEnum<>) ||
               type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
               type.GetGenericArguments()[0].IsGenericType &&
               type.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(StringEnum<>));
    }
}
