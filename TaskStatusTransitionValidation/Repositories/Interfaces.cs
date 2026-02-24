// ============================
// Repositories/Interfaces.cs
// ============================
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(int userId, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);

    Task<User> AddAsync(User user, CancellationToken ct);
    Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct);
}
public interface IProjectRepository
{
    Task<Project?> FindByIdAsync(int projectId, CancellationToken ct);
    Task<IReadOnlyList<Project>> ListAsync(string? q, bool? archived, CancellationToken ct);
    Task<Project> AddAsync(Project project, CancellationToken ct);
    Task<Project> UpdateAsync(Project project, CancellationToken ct);

    Task<bool> IsMemberAsync(int projectId, int userId, CancellationToken ct);
    Task<bool> IsAssigneeAllowedAsync(int projectId, int? assigneeUserId, CancellationToken ct);
    Task<IReadOnlyList<int>> GetMemberUserIdsAsync(int projectId, CancellationToken ct);

    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(
    int projectId,
    CancellationToken ct);
}

public interface ITaskRepository
{
    Task<TaskItem?> FindByIdAsync(int taskId, CancellationToken ct);
    Task<IReadOnlyList<TaskItem>> ListByProjectAsync(int projectId, CancellationToken ct);
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct);
    Task<TaskItem> UpdateAsync(TaskItem task, CancellationToken ct);
}
