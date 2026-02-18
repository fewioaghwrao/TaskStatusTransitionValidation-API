// ============================
// Controllers/DashboardController.cs (A-012)
// ============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly ICurrentUserService _current;
    private readonly IDashboardService _dashboard;

    public DashboardController(ICurrentUserService current, IDashboardService dashboard)
    {
        _current = current;
        _dashboard = dashboard;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary(CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        return Ok(await _dashboard.GetSummaryAsync(uid, ct));
    }
}
