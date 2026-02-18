// ============================
// Services/ProjectService.cs
// ============================
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectListItemResponse>> ListAsync(int currentUserId, string? q, bool? archived, CancellationToken ct);
    Task<ProjectDetailResponse> GetAsync(int currentUserId, int projectId, CancellationToken ct);
    Task<ProjectDetailResponse> CreateAsync(int currentUserId, ProjectCreateRequest req, CancellationToken ct);
    Task<ProjectDetailResponse> UpdateAsync(int currentUserId, int projectId, ProjectUpdateRequest req, CancellationToken ct);
}

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;

    public ProjectService(IProjectRepository projects) => _projects = projects;

    public async Task<IReadOnlyList<ProjectListItemResponse>> ListAsync(int currentUserId, string? q, bool? archived, CancellationToken ct)
    {
        // 要件：案件メンバー権限。ここでは「全案件からメンバーのみ表示」にしている（必要なら要件に合わせて調整）
        var all = await _projects.ListAsync(q, archived, ct);
        var list = new List<ProjectListItemResponse>();
        foreach (var p in all)
        {
            if (await _projects.IsMemberAsync(p.Id, currentUserId, ct))
            {
                list.Add(new ProjectListItemResponse { ProjectId = p.Id, Name = p.Name, IsArchived = p.IsArchived });
            }
        }
        return list;
    }

    public async Task<ProjectDetailResponse> GetAsync(int currentUserId, int projectId, CancellationToken ct)
    {
        var p = await _projects.FindByIdAsync(projectId, ct) ?? throw AppException.NotFound("projectId not found.");
        if (!await _projects.IsMemberAsync(projectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        return new ProjectDetailResponse { ProjectId = p.Id, Name = p.Name, IsArchived = p.IsArchived };
    }

    public async Task<ProjectDetailResponse> CreateAsync(int currentUserId, ProjectCreateRequest req, CancellationToken ct)
    {
        var created = await _projects.AddAsync(new Project { Name = req.Name }, ct);
        // メンバー追加は要件外（固定メンバー）だが、実運用では作成者を追加するのが自然。ここでは省略。
        return new ProjectDetailResponse { ProjectId = created.Id, Name = created.Name, IsArchived = created.IsArchived };
    }

    public async Task<ProjectDetailResponse> UpdateAsync(int currentUserId, int projectId, ProjectUpdateRequest req, CancellationToken ct)
    {
        var p = await _projects.FindByIdAsync(projectId, ct) ?? throw AppException.NotFound("projectId not found.");
        if (!await _projects.IsMemberAsync(projectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        p.Name = req.Name;
        if (req.IsArchived.HasValue) p.IsArchived = req.IsArchived.Value;

        var updated = await _projects.UpdateAsync(p, ct);
        return new ProjectDetailResponse { ProjectId = updated.Id, Name = updated.Name, IsArchived = updated.IsArchived };
    }
}
