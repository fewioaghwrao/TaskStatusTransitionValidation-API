// ============================
// Domain/Entities.cs
// ============================
namespace TaskStatusTransitionValidation.Domain;

public sealed class User
{
    public int Id { get; init; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
}

public sealed class Project
{
    public int Id { get; init; }
    public string Name { get; set; } = "";
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public sealed class ProjectMember
{
    public int ProjectId { get; init; }
    public int UserId { get; init; }
}

public sealed class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TaskState Status { get; set; } = TaskState.ToDo;
    public int? AssigneeUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}