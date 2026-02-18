// ============================
// Services/DashboardService.cs (A-012)
// ============================
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(int currentUserId, CancellationToken ct);
}

public sealed class DashboardService : IDashboardService
{
    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;

    public DashboardService(IProjectRepository projects, ITaskRepository tasks)
    {
        _projects = projects;
        _tasks = tasks;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(int currentUserId, CancellationToken ct)
    {
        // 要件に詳細がないため、ここでは「ユーザーがメンバーの案件の全タスク」を集計する簡易版
        // 期限切れ/近日期限/進捗サマリー（Should） :contentReference[oaicite:15]{index=15}
        var projects = await _projects.ListAsync(q: null, archived: false, ct);
        var targetProjectIds = new List<int>();
        foreach (var p in projects)
            if (await _projects.IsMemberAsync(p.Id, currentUserId, ct))
                targetProjectIds.Add(p.Id);

        var allTasks = new List<TaskItem>();
        foreach (var pid in targetProjectIds)
            allTasks.AddRange(await _tasks.ListByProjectAsync(pid, ct));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueSoonTo = today.AddDays(7); // 近日期限の閾値は未定義のため暫定

        int overdue = allTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != TaskState.Done);
        int dueSoon = allTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value >= today && t.DueDate.Value <= dueSoonTo && t.Status != TaskState.Done);

        var progress = new DashboardSummaryResponse.ProgressSummary
        {
            ToDo = allTasks.Count(t => t.Status == TaskState.ToDo),
            Doing = allTasks.Count(t => t.Status == TaskState.Doing),
            Blocked = allTasks.Count(t => t.Status == TaskState.Blocked),
            Done = allTasks.Count(t => t.Status == TaskState.Done),
        };

        return new DashboardSummaryResponse
        {
            OverdueCount = overdue,
            DueSoonCount = dueSoon,
            Progress = progress
        };
    }
}
