// Services/CurrentUserService.cs
using System.Security.Claims;
using TaskStatusTransitionValidation.Domain;

namespace TaskStatusTransitionValidation.Services;

public interface ICurrentUserService
{
    int GetRequiredUserId();
    UserRole GetRequiredUserRole();   // ★追加
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    public int GetRequiredUserId()
    {
        var user = _http.HttpContext?.User;

        var idStr =
            user?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user?.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var id))
            throw AppException.Unauthorized("Missing or invalid user id claim.");

        return id;
    }

    public UserRole GetRequiredUserRole()
    {
        var user = _http.HttpContext?.User;

        // ClaimTypes.Role に入れたのでまずそれを見る
        var roleStr = user?.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(roleStr))
            throw AppException.Unauthorized("Missing user role claim.");

        if (!Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            throw AppException.Unauthorized("Invalid user role claim.");

        return role;
    }
}
