using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubActionsDashboard.Api.Exceptions;
using GitHubActionsDashboard.Api.Handlers;
using GitHubActionsDashboard.Api.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Octokit;

const string ApiPrefix = "/api";

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddHttpContextAccessor();

/*builder.Services.AddHttpClient<IGitHubService, GitHubService>((services, options) =>
{
    var context = services.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is not available. Ensure IHttpContextAccessor is registered and used correctly.");
    var token = context.Session.GetString("github_access_token");
    var user = context.Session.GetString("github_user");

    if (String.IsNullOrEmpty(token)) throw new UnauthorizedException();

    options.BaseAddress = new Uri("https://api.github.com/");
    options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    options.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubActionsDashboard", "0.1"));
    options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});*/

builder.Services.AddTransient<IGitHubClient, GitHubClient>(services =>
{
    var context = services.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is not available. Ensure IHttpContextAccessor is registered and used correctly.");
    var token = context.Session.GetString("github_access_token");

    if (String.IsNullOrEmpty(token)) throw new UnauthorizedException();

    Connection connection = new(new ProductHeaderValue("GitHubActionsDashboard", "0.1"))
    {
        Credentials = new Credentials(token)
    };

    return new GitHubClient(connection);
});

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Remove /api prefix from all paths
        var newPaths = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiPathItem>();
        foreach (var path in document.Paths)
        {
            var newPath = path.Key.StartsWith(ApiPrefix) ? path.Key[ApiPrefix.Length..] : path.Key;
            newPaths[newPath] = path.Value;
        }
        document.Paths.Clear();
        foreach (var path in newPaths)
        {
            document.Paths.Add(path.Key, path.Value);
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".GitHub.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = (context) =>
    {
        var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is not null)
        {
            // Add exception details
            context.ProblemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            context.ProblemDetails.Extensions["exceptionMessage"] = exception.Message;

            // Include stack trace in development
            if (context.HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            {
                context.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace;
                context.ProblemDetails.Extensions["innerException"] = exception.InnerException?.ToString();
            }

            // Add custom properties for specific exception types
            if (exception is UnauthorizedException)
            {
                context.ProblemDetails.Extensions["authenticationRequired"] = true;
            }
        }

        // Add request information
        context.ProblemDetails.Extensions["requestId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        context.ProblemDetails.Extensions["path"] = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["method"] = context.HttpContext.Request.Method;
    };
});

builder.Services.AddExceptionHandler(options =>
{
    options.StatusCodeSelector = (ex) =>
    {
        if (ex is UnauthorizedException) return StatusCodes.Status401Unauthorized;
        return StatusCodes.Status500InternalServerError;
    };
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<RagStatus>());
});

var app = builder.Build();

/*app.Use((context, next) =>
{
    try
    {
        return next();
    }
    catch (UnauthorizedException)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsync($"An unexpected error occurred: {ex.Message}");
    }
});*/

app.UseExceptionHandler();


app.UseSession();
app.UseDefaultFiles();
app.MapStaticAssets();

app.MapOpenApi();

app.MapGet("/callback/github", CallbackHandler.Handle).ExcludeFromDescription();
app.MapGet("/login/github", LoginHandler.Handle).ExcludeFromDescription();

var api = app.MapGroup(ApiPrefix).WithOpenApi();

api.MapGet("repositories", RepositoriesHandler.Handle);
api.MapGet("repositories/grouped", GroupedRepositoriesHandler.Handle);
api.MapPost("workflows", WorkflowsHandler.Handle);
api.MapPost("workflows/runs", WorkflowRunsHandler.Handle);

app.MapFallbackToFile("/index.html");

app.Run();
