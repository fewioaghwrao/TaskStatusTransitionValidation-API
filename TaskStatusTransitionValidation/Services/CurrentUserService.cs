// ============================
// Services/CurrentUserService.cs
// ============================
using System.Security.Claims;

namespace TaskStatusTransitionValidation.Services;

public interface ICurrentUserService
{
    int GetRequiredUserId();
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
}

