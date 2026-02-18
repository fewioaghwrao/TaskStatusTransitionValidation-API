using Microsoft.EntityFrameworkCore;
using TaskStatusTransitionValidation.Domain;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Infrastructure;

public sealed class EfTaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    public EfTaskRepository(AppDbContext db) => _db = db;

    public async Task<TaskItem?> FindByIdAsync(int taskId, CancellationToken ct)
        => await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, ct); // 更新用途もあり得るので AsNoTracking は付けない

    public async Task<IReadOnlyList<TaskItem>> ListByProjectAsync(int projectId, CancellationToken ct)
        => await _db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct)
    {
        task.UpdatedAt = DateTime.UtcNow;

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task, CancellationToken ct)
    {
        task.UpdatedAt = DateTime.UtcNow;

        _db.Tasks.Update(task);
        await _db.SaveChangesAsync(ct);
        return task;
    }
}
