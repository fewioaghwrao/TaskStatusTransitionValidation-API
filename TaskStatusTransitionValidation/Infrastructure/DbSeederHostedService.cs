using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Infrastructure;

public sealed class DbSeederHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    public DbSeederHostedService(IServiceProvider sp) => _sp = sp;

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 既に投入済みなら何もしない（Usersが1件でもあればseed済み扱い）
        if (await db.Users.AnyAsync(ct)) return;

        // Users 10名（IdはDB採番に任せる）
        var users = new List<User>();
        for (int i = 1; i <= 10; i++)
        {
            var (hash, salt) = PasswordUtil.HashPassword("Demo1234!");
            users.Add(new User
            {
                Email = $"demo{i}@example.com",
                DisplayName = $"デモ作業者{i}",
                PasswordHash = hash,
                PasswordSalt = salt
            });
        }
        db.Users.AddRange(users);

        // Projects（IdはDB採番に任せる）
        var projects = new List<Project>
        {
            new() { Name="案件A：要件整理・設計", IsArchived=false, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow },
            new() { Name="案件B：API実装（認証）", IsArchived=false, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow },
            new() { Name="案件C：タスク管理機能", IsArchived=false, CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow },
        };
        db.Projects.AddRange(projects);

        // ★重要：ここで一旦保存して、Users/Projectsの採番Idを確定させる
        await db.SaveChangesAsync(ct);

        // ProjectMembers（各案件 5〜10人）
        var members = new List<ProjectMember>();
        foreach (var p in projects)
        {
            // 採番された p.Id を使う
            var rng = new Random(1000 + p.Id);
            var pickCount = 5 + (p.Id % 6); // 5〜10
            var picked = users.OrderBy(_ => rng.Next()).Take(pickCount).ToList();

            members.AddRange(picked.Select(u => new ProjectMember
            {
                ProjectId = p.Id,
                UserId = u.Id
            }));
        }
        db.ProjectMembers.AddRange(members);

        // Tasks（例: 150件。後で300に増やしてOK）
        var tasks = new List<TaskItem>();
        var taskCount = 150;

        var projectIds = projects.Select(p => p.Id).ToList();

        for (int n = 1; n <= taskCount; n++)
        {
            // 固定の 1..N ではなく、採番済み projectIds から割り当てる
            var projectId = projectIds[(n - 1) % projectIds.Count];

            var status = (n % 10) switch
            {
                0 => TaskState.Blocked,
                1 or 2 => TaskState.Done,
                3 or 4 => TaskState.Doing,
                _ => TaskState.ToDo
            };

            var priority = (n % 12) switch
            {
                0 or 1 => TaskPriority.High,
                2 => TaskPriority.Low,
                _ => TaskPriority.Medium
            };

            DateOnly? due =
                (n % 20) is 0 or 7 or 13
                    ? null
                    : DateOnly.FromDateTime(new DateTime(2026, 2, 1).AddDays(n % 120));

            int? assignee =
                (n % 17 == 0) ? null
                : PickMemberUserId(members, projectId, n);

            tasks.Add(new TaskItem
            {
                ProjectId = projectId,
                Title = $"Task-{n:0000}",
                Description = $"副業デモ用タスク。案件={projectId} / No={n}",
                Status = status,
                Priority = priority,
                DueDate = due,
                AssigneeUserId = assignee,

                // CreatedAt が init のままでも、初期化子ならOK
                CreatedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(n * 3),
                UpdatedAt = new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc).AddMinutes(n * 5),
            });
        }

        db.Tasks.AddRange(tasks);

        // ここでMembers/Tasksを保存
        await db.SaveChangesAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static int? PickMemberUserId(List<ProjectMember> members, int projectId, int seed)
    {
        var list = members
            .Where(m => m.ProjectId == projectId)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();

        if (list.Count == 0) return null;

        var rng = new Random(seed * 997);
        return list[rng.Next(list.Count)];
    }
}
