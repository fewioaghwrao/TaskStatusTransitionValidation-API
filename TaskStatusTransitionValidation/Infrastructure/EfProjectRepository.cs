using Microsoft.EntityFrameworkCore;
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Infrastructure;

public sealed class EfProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;

    public EfProjectRepository(AppDbContext db) => _db = db;

    public async Task<Project?> FindByIdAsync(int projectId, CancellationToken ct)
        => await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

    public async Task<IReadOnlyList<Project>> ListAsync(string? q, bool? archived, CancellationToken ct)
    {
        IQueryable<Project> query = _db.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q));

        if (archived.HasValue)
            query = query.Where(p => p.IsArchived == archived.Value);

        return await query.OrderBy(p => p.Id).ToListAsync(ct);
    }

    public async Task<Project> AddAsync(Project project, CancellationToken ct)
    {
        project.UpdatedAt = DateTime.UtcNow;

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> UpdateAsync(Project project, CancellationToken ct)
    {
        project.UpdatedAt = DateTime.UtcNow;

        _db.Projects.Update(project);
        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<bool> IsMemberAsync(int projectId, int userId, CancellationToken ct)
        => await _db.ProjectMembers.AsNoTracking()
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);

    public async Task<bool> IsAssigneeAllowedAsync(int projectId, int? assigneeUserId, CancellationToken ct)
    {
        if (assigneeUserId is null) return true;

        return await _db.ProjectMembers.AsNoTracking()
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == assigneeUserId.Value, ct);
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(
    int projectId,
    CancellationToken ct)
    {
        return await _db.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == projectId)
            .Join(
                _db.Users,
                pm => pm.UserId,
                u => u.Id,
                (pm, u) => new ProjectMemberDto
                {
                    UserId = u.Id,
                    DisplayName = u.DisplayName,
                    Email = u.Email
                }
            )
            .OrderBy(x => x.UserId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<int>> GetMemberUserIdsAsync(int projectId, CancellationToken ct)
        => await _db.ProjectMembers.AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);
}


