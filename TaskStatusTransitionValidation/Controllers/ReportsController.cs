// ============================
// Controllers/ReportsController.cs (A-011)
// ============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatusTransitionValidation.Domain;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ICurrentUserService _current;
    private readonly IReportService _reports;

    public ReportsController(ICurrentUserService current, IReportService reports)
    {
        _current = current;
        _reports = reports;
    }

    // GET /api/v1/reports/projects/{projectId}/tasks.csv
    // 期間フィルタ：from/to (yyyy-MM-dd), status複数指定可能 :contentReference[oaicite:19]{index=19}
    [HttpGet("projects/{projectId:int}/tasks.csv")]
    public async Task<IActionResult> ExportProjectTasksCsv(
        int projectId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery(Name = "status")] TaskState[]? statuses,
        CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var result = await _reports.ExportProjectTasksCsvAsync(uid, projectId, from, to, statuses, ct);
        return File(result.Bytes, "text/csv; charset=utf-8", result.FileName);

    }

}
