using Microsoft.EntityFrameworkCore;
using TaskStatusTransitionValidation.Domain;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Infrastructure;

public sealed class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public EfUserRepository(AppDbContext db) => _db = db;

    public async Task<User?> FindByIdAsync(int userId, CancellationToken ct)
        => await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct)
        => await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct)
    {
        // user.PasswordHash / PasswordSalt は呼び出し側でセット済み想定
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, ct);

        if (u is null) return false;

        // 既存の PasswordUtil を利用（あなたの InMemory 実装と同じ）
        return PasswordUtil.VerifyPassword(password, u.PasswordHash, u.PasswordSalt);
    }
}
