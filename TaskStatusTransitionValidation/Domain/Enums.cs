// ============================
// Domain/Enums.cs
// ============================
namespace TaskStatusTransitionValidation.Domain;

public enum TaskState
{
    ToDo,
    Doing,
    Blocked,
    Done
}

public enum TaskPriority
{
    High,
    Medium,
    Low
}

// CSVの並び順で Status（ToDo → Doing → Blocked → Done）を要求 :contentReference[oaicite:2]{index=2}
public static class SortOrders
{
    public static int StatusOrder(TaskState s) => s switch
    {
        TaskState.ToDo => 1,
        TaskState.Doing => 2,
        TaskState.Blocked => 3,
        TaskState.Done => 4,
        _ => 999
    };

    public static int PriorityOrder(TaskPriority p) => p switch
    {
        TaskPriority.High => 1,
        TaskPriority.Medium => 2,
        TaskPriority.Low => 3,
        _ => 999
    };
}