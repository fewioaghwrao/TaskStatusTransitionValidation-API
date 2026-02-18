// ============================
// Services/TaskService.cs
// ============================
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(int currentUserId, int projectId, CancellationToken ct);
    Task<TaskResponse> CreateAsync(int currentUserId, TaskCreateRequest req, CancellationToken ct);
    Task<TaskResponse> UpdateAsync(int currentUserId, int taskId, TaskUpdateRequest req, CancellationToken ct);

}

public sealed class TaskService : ITaskService
{
    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly ITaskStatusTransitionPolicy _policy;

    // 推奨：Doneのタスクは更新不可 :contentReference[oaicite:5]{index=5}
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

    public async Task<IReadOnlyList<TaskResponse>> ListByProjectAsync(int currentUserId, int projectId, CancellationToken ct)
    {
        // プロジェクト存在チェック
        var p = await _projects.FindByIdAsync(projectId, ct);
        if (p is null) throw AppException.NotFound("Project not found.");

        // メンバーでなければ見れない
        var isMember = await _projects.IsMemberAsync(projectId, currentUserId, ct);
        if (!isMember) throw AppException.Forbidden("You are not a member of this project.");

        var list = await _tasks.ListByProjectAsync(projectId, ct);

        // 返却DTOへ
        return list.Select(MapToResponse).ToList();
    }
    private static TaskResponse MapToResponse(TaskItem t)
    {
        return new TaskResponse
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
    }

    public async Task<TaskResponse> CreateAsync(int currentUserId, TaskCreateRequest req, CancellationToken ct)
    {
        // A-009: project存在、ログイン、メンバー権限、assignee存在/メンバーチェック、statusはToDo固定 :contentReference[oaicite:6]{index=6}
        var project = await _projects.FindByIdAsync(req.ProjectId, ct) ?? throw AppException.NotFound("projectId not found.");
        if (!await _projects.IsMemberAsync(req.ProjectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

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

            // 新規作成時 status は常に ToDo :contentReference[oaicite:7]{index=7}
            Status = TaskState.ToDo
        };

        var created = await _tasks.AddAsync(entity, ct);
        return Map(created);
    }

    public async Task<TaskResponse> UpdateAsync(int currentUserId, int taskId, TaskUpdateRequest req, CancellationToken ct)
    {
        // A-010: task存在、案件メンバー、状態遷移チェック、assigneeは案件メンバーのみ :contentReference[oaicite:8]{index=8}
        var task = await _tasks.FindByIdAsync(taskId, ct) ?? throw AppException.NotFound("taskId not found.");

        if (!await _projects.IsMemberAsync(task.ProjectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

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

        // 状態遷移チェック（推奨：違反は409） :contentReference[oaicite:9]{index=9}
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
}
