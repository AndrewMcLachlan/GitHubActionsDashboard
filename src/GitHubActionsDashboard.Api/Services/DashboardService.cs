﻿using System.Net;
using System.Text.Json;
using GitHubActionsDashboard.Api.Models;
using GitHubActionsDashboard.Api.Models.Dashboard;
using GitHubActionsDashboard.Api.Requests;
using Microsoft.Extensions.Caching.Distributed;
using Octokit;

namespace GitHubActionsDashboard.Api.Services;

public interface IDashboardService
{
    Task<IEnumerable<RepositoryModel>> GetWorkflowsAsync(WorkflowsRequest request, CancellationToken cancellationToken);

    Task<IEnumerable<WorkflowModel>> GetWorkflowsAsync(string owner, string repo, CancellationToken cancellationToken);

    Task<IEnumerable<WorkflowRunModel>> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, string? branch, CancellationToken cancellationToken);

    Task<IEnumerable<WorkflowRunModel>> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, IEnumerable<string> branches, CancellationToken cancellationToken);
}

internal class DashboardService(IGitHubClient gitHubClient, IDistributedCache cache, ICacheKeyService cacheKeyService, ILogger<SettingsService> logger) : GitHubService(cache, logger), IDashboardService
{
    private readonly SemaphoreSlim _gate = new(8);

    public async Task<IEnumerable<RepositoryModel>> GetWorkflowsAsync(WorkflowsRequest request, CancellationToken cancellationToken)
    {
        List<Task<Repository>> repoTasks = [.. request.Repositories.Select(repo => gitHubClient.Repository.Get(repo.Owner, repo.Name))];
        var workflowIds = request.Repositories.SelectMany(r => r.Workflows);

        Dictionary<Repository, IEnumerable<WorkflowModel>> workflows = [];
        Dictionary<Repository, Task<IEnumerable<WorkflowModel>>> workflowTasks = [];

        List<Task<IEnumerable<WorkflowRunModel>>> runsTasks = [];
        List<WorkflowRunModel> workflowRuns = [];

        List<RepositoryModel> results = [];

        var repositories = await Task.WhenAll(repoTasks);

        foreach (var repo in repositories)
        {
            workflowTasks.Add(repo, GetWorkflowsAsync(repo.Owner.Name ?? repo.Owner.Login, repo.Name, cancellationToken));
        }

        await Task.WhenAll(workflowTasks.Values);

        foreach (var task in workflowTasks)
        {
            workflows.Add(task.Key, [.. task.Value.Result.Where(wf => workflowIds.Any(id => id == wf.Id)).OrderBy(w => w.Name)]);
        }

        foreach (var repo in repositories)
        {
            //foreach (var workflow in repo.Workflows[repo].Select(w => w.Id))
            foreach (var workflow in workflows[repo])
            {
                runsTasks.Add(GetLastRunsAsync(repo.Owner.Login, repo.Name, workflow.Id, 1, repo.DefaultBranch, cancellationToken));
            }
        }

        await Task.WhenAll(runsTasks);

        foreach (var runTask in runsTasks)
        {
            workflowRuns.AddRange(runTask.Result);
        }

        foreach (var workflowRepo in workflows)
        {
            results.Add(new RepositoryModel
            {
                Name = workflowRepo.Key.Name,
                Owner = workflowRepo.Key.Owner.Name ?? workflowRepo.Key.Owner.Login,
                NodeId = workflowRepo.Key.NodeId,
                HtmlUrl = workflowRepo.Key.HtmlUrl,
                Workflows = workflowRepo.Value.Select(workflow => workflow with { Runs = [.. workflowRuns.Where(run => run.WorkflowId == workflow.Id)] }).OrderBy(workflow => workflow.Name),
            });
        }

        return results;
    }

    public async Task<IEnumerable<WorkflowModel>> GetWorkflowsAsync(string owner, string repo, CancellationToken cancellationToken)
    {
        var cacheKey = cacheKeyService.GetCacheKey($"gh:workflows:{owner}/{repo}");

        var cachedWorkflows = await TryGetFromCache<WorkflowModel>(cacheKey, cancellationToken);
        if (cachedWorkflows != null)
        {
            return cachedWorkflows;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await Jitter(cancellationToken);
            var response = await OctoCall(() => gitHubClient.Actions.Workflows.List(owner, repo), cancellationToken);

            var workflows = response.Workflows.ToDashboardWorkflowModel();

            await TryCache(cacheKey, workflows, cancellationToken);

            return workflows;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IEnumerable<WorkflowRunModel>> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, string? branch, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await Jitter(cancellationToken);
            var req = new Octokit.WorkflowRunsRequest
            {
                Branch = branch,

            };

            var response = await OctoCall(() =>
                gitHubClient.Actions.Workflows.Runs.ListByWorkflow(owner, repo, workflowId, req,
                    new ApiOptions
                    {
                        PageCount = 1,
                        PageSize = perPage,
                        StartPage = 1,
                    }), cancellationToken);

            return response.WorkflowRuns.Select(wr => new WorkflowRunModel()
            {
                Id = wr.Id,
                WorkflowId = wr.WorkflowId,
                NodeId = wr.NodeId,
                Conclusion = wr.Conclusion,
                CreatedAt = wr.CreatedAt,
                Event = wr.Event,
                HeadBranch = wr.HeadBranch,
                HtmlUrl = wr.HtmlUrl,
                RunNumber = wr.RunNumber,
                Status = wr.Status,
                TriggeringActor = wr.TriggeringActor?.Name ?? wr.TriggeringActor?.Login,
                UpdatedAt = wr.UpdatedAt,
            });
        }
        finally { _gate.Release(); }
    }

    public async Task<IEnumerable<WorkflowRunModel>> GetLastRunsAsync(string owner, string repo, long workflowId, int perPage, IEnumerable<string> branches, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            string? branch = null;
            if (branches.Count() == 1 && !branches.First().Contains('*'))
            {
                branch = branches.First();
            }

            await Jitter(cancellationToken);
            var req = new Octokit.WorkflowRunsRequest
            {
                Branch = branch,
            };

            var response = await OctoCall(() =>
                gitHubClient.Actions.Workflows.Runs.ListByWorkflow(owner, repo, workflowId, req,
                    new ApiOptions
                    {
                        PageCount = 1,
                        PageSize = perPage,
                        StartPage = 1,
                    }), cancellationToken);

            return response.WorkflowRuns
                .Where(wr => MatchBranch(wr, branches))
                .Select(wr => new WorkflowRunModel()
                {
                    Id = wr.Id,
                    WorkflowId = wr.WorkflowId,
                    NodeId = wr.NodeId,
                    Conclusion = wr.Conclusion,
                    CreatedAt = wr.CreatedAt,
                    Event = wr.Event,
                    HeadBranch = wr.HeadBranch,
                    HtmlUrl = wr.HtmlUrl,
                    RunNumber = wr.RunNumber,
                    Status = wr.Status,
                    TriggeringActor = wr.TriggeringActor?.Name ?? wr.TriggeringActor?.Login,
                    UpdatedAt = wr.UpdatedAt,
                });
        }
        finally { _gate.Release(); }
    }

    private static bool MatchBranch(WorkflowRun workflowRun, IEnumerable<string> branchFilters)
    {
        if (!branchFilters.Any()) return true;

        if (branchFilters.Contains(workflowRun.HeadBranch)) return true;

        var startsWith = branchFilters.Where(b => b.EndsWith('*')).Select(b => b.Trim('*'));

        return startsWith.Any(workflowRun.HeadBranch.StartsWith);
    }
}
