using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ICurrentUserService _current;

    public UsersController(ICurrentUserService current) => _current = current;

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        // userId は既存ロジックを流用（sub/nameidentifier整合）
        var userId = _current.GetRequiredUserId();

        // JWTに入ってるものを返す（今の設計だとこれが最短）
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
        var displayName = User.FindFirstValue("displayName") ?? "";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Worker";
        var roleId = User.FindFirstValue("roleId"); 

        return Ok(new
        {
            userId,
            email,
            displayName,
            role,
            roleId
        });
    }
}
