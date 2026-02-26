// ============================
// Services/TaskService.cs
// ============================
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(int currentUserId, UserRole role, int projectId, CancellationToken ct);
    Task<TaskResponse> CreateAsync(int currentUserId, UserRole role, TaskCreateRequest req, CancellationToken ct);
    Task<TaskResponse> UpdateAsync(int currentUserId, UserRole role, int taskId, TaskUpdateRequest req, CancellationToken ct);

    Task<TaskResponse> GetAsync(int currentUserId, UserRole role, int taskId, CancellationToken ct);
}

public sealed class TaskService : ITaskService
{
    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly ITaskStatusTransitionPolicy _policy;

    private const bool DisallowAnyUpdateWhenDone = true;

    public TaskService(
        IProjectRepository projects,
        ITaskRepository tasks,
        IUserRepository users,
        ITaskStatusTransitionPolicy policy)
    {
        _projects = projects;
        _tasks = tasks;
        _users = users;
        _policy = policy;
    }

    public async Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(
        int currentUserId,
        UserRole role,
        int projectId,
        CancellationToken ct)
    {
        // プロジェクト存在チェック
        var p = await _projects.FindByIdAsync(projectId, ct);
        if (p is null) throw AppException.NotFound("Project not found.");

        // Leader以外はメンバー必須
        if (role != UserRole.Leader)
        {
            var isMember = await _projects.IsMemberAsync(projectId, currentUserId, ct);
            if (!isMember) throw AppException.Forbidden("You are not a member of this project.");
        }

        var list = await _tasks.ListByProjectAsync(projectId, ct);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<TaskResponse> CreateAsync(
        int currentUserId,
        UserRole role,
        TaskCreateRequest req,
        CancellationToken ct)
    {
        // project存在
        _ = await _projects.FindByIdAsync(req.ProjectId, ct) ?? throw AppException.NotFound("projectId not found.");

        // Leader以外はメンバー必須
        if (role != UserRole.Leader && !await _projects.IsMemberAsync(req.ProjectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        if (role != UserRole.Leader && req.AssigneeUserId.HasValue)
            throw AppException.Forbidden("Worker cannot set assigneeUserId. It must be null.");

        // assigneeの存在/メンバー確認（Leaderでも “assigneeは案件メンバーのみ” を維持）
        if (req.AssigneeUserId.HasValue)
        {
            var assignee = await _users.FindByIdAsync(req.AssigneeUserId.Value, ct);
            if (assignee is null) throw AppException.NotFound("assigneeUserId not found.");

            var ok = await _projects.IsAssigneeAllowedAsync(req.ProjectId, req.AssigneeUserId, ct);
            if (!ok) throw AppException.BadRequest("assigneeUserId must be a project member.");
        }

        var entity = new TaskItem
        {
            ProjectId = req.ProjectId,
            Title = req.Title,
            Description = req.Description,
            AssigneeUserId = req.AssigneeUserId,
            DueDate = req.DueDate,
            Priority = req.Priority,
            Status = TaskState.ToDo
        };

        var created = await _tasks.AddAsync(entity, ct);
        return Map(created);
    }

    public async Task<TaskResponse> UpdateAsync(
        int currentUserId,
        UserRole role,
        int taskId,
        TaskUpdateRequest req,
        CancellationToken ct)
    {
        var task = await _tasks.FindByIdAsync(taskId, ct) ?? throw AppException.NotFound("taskId not found.");

        // Leader以外はメンバー必須
        if (role != UserRole.Leader && !await _projects.IsMemberAsync(task.ProjectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        if (role != UserRole.Leader && req.AssigneeUserId.HasValue)
            throw AppException.Forbidden("Worker cannot set assigneeUserId. It must be null.");

        if (DisallowAnyUpdateWhenDone && task.Status == TaskState.Done)
            throw AppException.Conflict("Done task cannot be updated (policy).", new()
            {
                ["currentStatus"] = task.Status.ToString()
            });

        // assigneeの存在/メンバー確認
        if (req.AssigneeUserId.HasValue)
        {
            var assignee = await _users.FindByIdAsync(req.AssigneeUserId.Value, ct);
            if (assignee is null) throw AppException.NotFound("assigneeUserId not found.");

            var ok = await _projects.IsAssigneeAllowedAsync(task.ProjectId, req.AssigneeUserId, ct);
            if (!ok) throw AppException.BadRequest("assigneeUserId must be a project member.");
        }

        // 状態遷移チェック
        if (!_policy.CanTransition(task.Status, req.Status))
        {
            throw AppException.Conflict("Invalid status transition.", new()
            {
                ["from"] = task.Status.ToString(),
                ["to"] = req.Status.ToString(),
                ["allowed"] = new[] { "ToDo->Doing", "Doing->Done", "Doing->Blocked", "Blocked->Doing" }
            });
        }

        // 更新
        task.Title = req.Title;
        task.Description = req.Description;
        task.AssigneeUserId = req.AssigneeUserId;
        task.DueDate = req.DueDate;
        task.Priority = req.Priority;
        task.Status = req.Status;

        var updated = await _tasks.UpdateAsync(task, ct);
        return Map(updated);
    }

    private static TaskResponse MapToResponse(TaskItem t) => new()
    {
        TaskId = t.Id,
        ProjectId = t.ProjectId,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        DueDate = t.DueDate,
        AssigneeUserId = t.AssigneeUserId,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    private static TaskResponse Map(TaskItem t) => new()
    {
        TaskId = t.Id,
        ProjectId = t.ProjectId,
        Title = t.Title,
        Status = t.Status,
        AssigneeUserId = t.AssigneeUserId,
        DueDate = t.DueDate,
        Priority = t.Priority,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    public async Task<TaskResponse> GetAsync(int currentUserId, UserRole role, int taskId, CancellationToken ct)
    {
        var task = await _tasks.FindByIdAsync(taskId, ct) ?? throw AppException.NotFound("taskId not found.");

        if (role != UserRole.Leader && !await _projects.IsMemberAsync(task.ProjectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        return MapToResponse(task);
    }
}