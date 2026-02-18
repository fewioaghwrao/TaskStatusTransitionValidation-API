namespace TaskStatusTransitionValidation.Domain;

public sealed class TaskComment
{
    public int Id { get; init; }
    public int TaskId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

