﻿using GitHubActionsDashboard.Api.Exceptions;
using GitHubActionsDashboard.Api.Services;

namespace GitHubActionsDashboard.Api.Extensions;

public static class GraphQLServiceCollectionExtensions
{
    public static IServiceCollection AddGraphQLServices(this IServiceCollection services) =>
        services
            .AddScoped<IGraphQLService, GraphQLService>()
            .AddScoped(services =>
            {
                var context = services.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is not available. Ensure IHttpContextAccessor is registered and used correctly.");
                var token = context.Session.GetString("github_access_token");

                if (String.IsNullOrEmpty(token)) throw new UnauthorizedException();

                Octokit.GraphQL.Connection connection = new(new Octokit.GraphQL.ProductHeaderValue("GitHubActionsDashboard", "0.1"), token);

                return connection;
            });
}
