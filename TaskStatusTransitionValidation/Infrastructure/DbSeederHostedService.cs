using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Infrastructure;

/// <summary>
/// 初回データ投入用 Seeder。
///
/// ■想定用途
/// - ローカル開発環境での初回データ作成
/// - Azure / 公開環境で「初回だけ」手動フラグで Seed したい場合
///
/// ■重要
/// このクラスは Azure 安定化のため、常時 AddHostedService で自動登録して
/// 毎回起動時に走らせる運用は推奨しない。
///
/// 理由:
/// - アプリ起動のたびに DB 接続が発生する
/// - Azure SQL / SQL Server 接続の一時的不安定で startup failure の原因になり得る
/// - 500.30 のような起動失敗と相性が悪い
///
/// そのため、Program.cs 側で
///   ENABLE_DB_SEED = true
/// のときだけ明示実行する運用を想定する。
/// </summary>
public sealed class DbSeederHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<DbSeederHostedService> _logger;

    public DbSeederHostedService(
        IServiceProvider sp,
        ILogger<DbSeederHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("DbSeederHostedService started.");

        try
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 既に投入済みなら何もしない。
            // Users が1件でもあれば seed 済みとみなす。
            //
            // ※ 毎回起動時に走る設計ではなく「初回だけ必要時に呼ぶ」前提だが、
            //    念のため冪等性を持たせている。
            var alreadySeeded = await db.Users.AnyAsync(ct);
            if (alreadySeeded)
            {
                _logger.LogInformation("Seed skipped because data already exists.");
                return;
            }

            _logger.LogInformation("Seeding demo users...");
            var users = new List<User>();
            for (int i = 1; i <= 10; i++)
            {
                var (hash, salt) = PasswordUtil.HashPassword("Demo1234!");
                users.Add(new User
                {
                    Email = $"demo{i}@example.com",
                    DisplayName = i == 1 ? "デモリーダー" : $"デモ作業者{i}",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = i == 1 ? UserRole.Leader : UserRole.Worker
                });
            }
            db.Users.AddRange(users);

            _logger.LogInformation("Seeding demo projects...");
            var projects = new List<Project>
            {
                new()
                {
                    Name = "案件A：要件整理・設計",
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "案件B：API実装（認証）",
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "案件C：タスク管理機能",
                    IsArchived = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
            };
            db.Projects.AddRange(projects);

            // ここで一旦保存し、Users / Projects の採番 ID を確定させる。
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Seeding project members...");
            var members = new List<ProjectMember>();
            foreach (var p in projects)
            {
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

            var projectNameById = projects.ToDictionary(p => p.Id, p => p.Name);
            var userNameById = users.ToDictionary(u => u.Id, u => u.DisplayName);

            _logger.LogInformation("Seeding tasks...");
            var tasks = new List<TaskItem>();
            var taskCount = 150;
            var projectIds = projects.Select(p => p.Id).ToList();

            for (int n = 1; n <= taskCount; n++)
            {
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

                var projectName = projectNameById.TryGetValue(projectId, out var pn)
                    ? pn
                    : $"案件#{projectId}";

                var assigneeName =
                    assignee is null
                        ? "未割当"
                        : (userNameById.TryGetValue(assignee.Value, out var un)
                            ? un
                            : $"User#{assignee.Value}");

                tasks.Add(new TaskItem
                {
                    ProjectId = projectId,
                    Title = $"作業-{n:0000}",
                    Description = $"案件：{projectName} / 担当：{assigneeName} / No={n}",
                    Status = status,
                    Priority = priority,
                    DueDate = due,
                    AssigneeUserId = assignee,
                    CreatedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(n * 3),
                    UpdatedAt = new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc).AddMinutes(n * 5),
                });
            }

            db.Tasks.AddRange(tasks);

            // Members / Tasks を保存
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Database seed completed successfully. Users={UserCount}, Projects={ProjectCount}, Tasks={TaskCount}",
                users.Count,
                projects.Count,
                tasks.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Database seed was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            // 起動時トラブルの切り分けをしやすくするため、
            // 例外内容は必ずログへ残す。
            _logger.LogError(ex, "Database seed failed.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("DbSeederHostedService stopped.");
        return Task.CompletedTask;
    }

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
