// ============================
// Services/ProjectService.cs
// ============================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;
using TaskStatusTransitionValidation.Infrastructure;

namespace TaskStatusTransitionValidation.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectListItemResponse>> ListAsync(
        int currentUserId,
        UserRole role,
        string? q,
        bool? archived,
        CancellationToken ct);

    Task<ProjectDetailResponse> GetAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        CancellationToken ct);

    Task<ProjectDetailResponse> CreateAsync(
        int currentUserId,
        ProjectCreateRequest req,
        CancellationToken ct);

    Task<ProjectDetailResponse> UpdateAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        ProjectUpdateRequest req,
        CancellationToken ct);

    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        CancellationToken ct);

    Task ArchiveAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        CancellationToken ct);
}

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;

    public ProjectService(IProjectRepository projects) => _projects = projects;

    public async Task<IReadOnlyList<ProjectListItemResponse>> ListAsync(
        int currentUserId,
        UserRole role,
        string? q,
        bool? archived,
        CancellationToken ct)
    {
        var all = await _projects.ListAsync(q, archived, ct);

        if (role == UserRole.Leader)
        {
            return all.Select(p => new ProjectListItemResponse
            {
                ProjectId = p.Id,
                Name = p.Name,
                IsArchived = p.IsArchived
            }).ToList();
        }

        var list = new List<ProjectListItemResponse>();
        foreach (var p in all)
        {
            if (await _projects.IsMemberAsync(p.Id, currentUserId, ct))
            {
                list.Add(new ProjectListItemResponse
                {
                    ProjectId = p.Id,
                    Name = p.Name,
                    IsArchived = p.IsArchived
                });
            }
        }

        return list;
    }

    public async Task<ProjectDetailResponse> GetAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        CancellationToken ct)
    {
        var p = await _projects.FindByIdAsync(projectId, ct)
            ?? throw AppException.NotFound("projectId not found.");

        if (role != UserRole.Leader && !await _projects.IsMemberAsync(projectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        return new ProjectDetailResponse
        {
            ProjectId = p.Id,
            Name = p.Name,
            IsArchived = p.IsArchived
        };
    }

    public async Task<ProjectDetailResponse> CreateAsync(
        int currentUserId,
        ProjectCreateRequest req,
        CancellationToken ct)
    {
        var created = await _projects.AddAsync(new Project { Name = req.Name }, ct);

        return new ProjectDetailResponse
        {
            ProjectId = created.Id,
            Name = created.Name,
            IsArchived = created.IsArchived
        };
    }

    public async Task<ProjectDetailResponse> UpdateAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        ProjectUpdateRequest req,
        CancellationToken ct)
    {
        var p = await _projects.FindByIdAsync(projectId, ct)
            ?? throw AppException.NotFound("projectId not found.");

        if (role != UserRole.Leader && !await _projects.IsMemberAsync(projectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        p.Name = req.Name;
        if (req.IsArchived.HasValue) p.IsArchived = req.IsArchived.Value;

        var updated = await _projects.UpdateAsync(p, ct);

        return new ProjectDetailResponse
        {
            ProjectId = updated.Id,
            Name = updated.Name,
            IsArchived = updated.IsArchived
        };
    }

    public async Task ArchiveAsync(int currentUserId, UserRole role, int projectId, CancellationToken ct)
    {
        if (role != UserRole.Leader)
            throw AppException.Forbidden("Leader role required.");

        var p = await _projects.FindByIdAsync(projectId, ct)
            ?? throw AppException.NotFound("projectId not found.");

        if (p.IsArchived) return;

        p.IsArchived = true;
        await _projects.UpdateAsync(p, ct);
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        CancellationToken ct)
    {
        // LeaderはOK、Workerはその案件メンバーのみOK（GetAsyncと揃える）
        if (role != UserRole.Leader && !await _projects.IsMemberAsync(projectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        // ✅ ここが修正点：_projects を呼ぶ
        return await _projects.GetMembersAsync(projectId, ct);
    }
}