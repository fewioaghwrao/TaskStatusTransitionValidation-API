// ============================
// Services/ReportService.cs  (CSV出力: A-011)
// ============================
using System.Linq;
using System.Text;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface IReportService
{
    Task<(byte[] Bytes, string FileName)> ExportProjectTasksCsvAsync(
        int currentUserId,
        int projectId,
        DateOnly? from,
        DateOnly? to,
        TaskState[]? statuses,   // ★ TaskStatus -> TaskState
        CancellationToken ct);
}

public sealed class ReportService : IReportService
{
    private readonly IProjectRepository _projects;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;

    public ReportService(IProjectRepository projects, ITaskRepository tasks, IUserRepository users)
    {
        _projects = projects;
        _tasks = tasks;
        _users = users;
    }

    public async Task<(byte[] Bytes, string FileName)> ExportProjectTasksCsvAsync(
        int currentUserId,
        int projectId,
        DateOnly? from,
        DateOnly? to,
        TaskState[]? statuses,   // ★ TaskStatus -> TaskState
        CancellationToken ct)
    {
        var project = await _projects.FindByIdAsync(projectId, ct) ?? throw AppException.NotFound("projectId not found.");
        if (!await _projects.IsMemberAsync(projectId, currentUserId, ct))
            throw AppException.Forbidden("You are not a member of this project.");

        var list = await _tasks.ListByProjectAsync(projectId, ct);

        if (from.HasValue || to.HasValue)
        {
            list = list.Where(t =>
            {
                if (!t.DueDate.HasValue) return false;
                if (from.HasValue && t.DueDate.Value < from.Value) return false;
                if (to.HasValue && t.DueDate.Value > to.Value) return false;
                return true;
            }).ToList();
        }

        if (statuses is { Length: > 0 })
        {
            var set = statuses.ToHashSet();
            list = list.Where(t => set.Contains(t.Status)).ToList(); // t.Status は TaskState
        }

        var sorted = list
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => SortOrders.StatusOrder(t.Status)) // SortOrders側も TaskState を受けるようにしておく
            .ThenBy(t => SortOrders.PriorityOrder(t.Priority))
            .ThenByDescending(t => t.UpdatedAt)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("ProjectName,TaskId,Title,Status,Priority,Assignee,DueDate,CreatedAt,UpdatedAt\r\n");

        foreach (var t in sorted)
        {
            var assigneeName = "Unassigned";
            if (t.AssigneeUserId.HasValue)
            {
                var u = await _users.FindByIdAsync(t.AssigneeUserId.Value, ct);
                if (u is not null) assigneeName = u.DisplayName;
            }

            sb.Append(Csv(project.Name)).Append(',')
              .Append(t.Id).Append(',')
              .Append(Csv(t.Title)).Append(',')
              .Append(t.Status).Append(',')
              .Append(t.Priority).Append(',')
              .Append(Csv(assigneeName)).Append(',')
              .Append(t.DueDate?.ToString("yyyy-MM-dd") ?? "").Append(',')
              .Append(t.CreatedAt.ToString("yyyy-MM-dd HH:mm")).Append(',')
              .Append(t.UpdatedAt.ToString("yyyy-MM-dd HH:mm")).Append("\r\n");
        }

        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8Bom.GetBytes(sb.ToString());

        var fileName = $"project_{projectId}_tasks_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        return (bytes, fileName);
    }

    private static string Csv(string? value)
    {
        value ??= "";
        var mustQuote = value.Contains(',') || value.Contains('\r') || value.Contains('\n') || value.Contains('"');
        if (!mustQuote) return value;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
