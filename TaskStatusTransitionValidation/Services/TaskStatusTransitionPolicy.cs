// ============================
// Services/TaskStatusTransitionPolicy.cs
// ============================
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

// 要件：許可遷移
// - ToDo → Doing
// - Doing → Done
// - Doing → Blocked
// - Blocked → Doing
// Done は終端（Done→他は禁止） :contentReference[oaicite:4]{index=4}
public interface ITaskStatusTransitionPolicy
{
    bool CanTransition(TaskState from, TaskState to);
}

public sealed class TaskStatusTransitionPolicy : ITaskStatusTransitionPolicy
{
    private static readonly HashSet<(TaskState From, TaskState To)> Allowed = new()
    {
        (TaskState.ToDo, TaskState.Doing),
        (TaskState.Doing, TaskState.Done),
        (TaskState.Doing, TaskState.Blocked),
        (TaskState.Blocked, TaskState.Doing),
    };

    public bool CanTransition(TaskState from, TaskState to)
    {
        if (from == to) return true; // 同一なら許容（内容更新のみ等）
        if (from == TaskState.Done) return false;
        return Allowed.Contains((from, to));
    }
}
