// ============================
// Contracts/DTOs.cs
// ============================
using System.ComponentModel.DataAnnotations;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Contracts;

public sealed class AuthLoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public sealed class AuthRegisterRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string DisplayName { get; set; } = "";
}

public sealed class AuthTokenResponse
{
    public string Token { get; set; } = "";
}

public sealed class ProjectListItemResponse
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public bool IsArchived { get; set; }
}

public sealed class ProjectCreateRequest
{
    [Required] public string Name { get; set; } = "";
}

public sealed class ProjectUpdateRequest
{
    [Required] public string Name { get; set; } = "";
    public bool? IsArchived { get; set; }
}

public sealed class ProjectDetailResponse
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public bool IsArchived { get; set; }
}

public sealed class TaskCreateRequest
{
    [Required] public int ProjectId { get; set; }
    [Required] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? AssigneeUserId { get; set; }

    // 要件：yyyy-MM-dd（例: "2026-03-01"） :contentReference[oaicite:3]{index=3}
    public DateOnly? DueDate { get; set; }

    [Required] public TaskPriority Priority { get; set; }
}

public sealed class TaskUpdateRequest
{
    [Required] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? AssigneeUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    [Required] public TaskPriority Priority { get; set; }

    [Required]
    public TaskState Status { get; set; }   // ★ ここを TaskState に
}

public sealed class TaskResponse
{
    public int TaskId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public TaskState Status { get; set; }   // ★ TaskStatus ではなく TaskState にする

    public int? AssigneeUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class DashboardSummaryResponse
{
    public int OverdueCount { get; set; }
    public int DueSoonCount { get; set; }
    public ProgressSummary Progress { get; set; } = new();
    public sealed class ProgressSummary
    {
        public int ToDo { get; set; }
        public int Doing { get; set; }
        public int Blocked { get; set; }
        public int Done { get; set; }
    }
}

public sealed class ProjectMemberDto
{
    public int UserId { get; init; }
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
}
