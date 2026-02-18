// ============================
// Controllers/AuthController.cs (A-001..A-003)
// ============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Domain;
using TaskStatusTransitionValidation.Infrastructure;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _jwt;

    public AuthController(IUserRepository users, IJwtTokenService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] AuthLoginRequest req, CancellationToken ct)
    {
        var ok = await _users.VerifyPasswordAsync(req.Email, req.Password, ct);
        if (!ok) throw AppException.Unauthorized("Invalid email or password.");

        var u = await _users.FindByEmailAsync(req.Email, ct);
        if (u is null) throw AppException.Unauthorized("Invalid email or password.");

        var token = _jwt.CreateToken(u);
        return Ok(new AuthTokenResponse { Token = token });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] AuthRegisterRequest req, CancellationToken ct)
    {
        var exists = await _users.FindByEmailAsync(req.Email, ct);
        if (exists is not null) throw AppException.BadRequest("Email already exists.");

        // hashはサンプル
        var (hash, salt) = PasswordUtil.HashPassword(req.Password);

        var user = new User
        {
            Email = req.Email,
            DisplayName = req.DisplayName,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await _users.AddAsync(user, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Cookie無効化などは環境側で対応
        return Ok();
    }
}


