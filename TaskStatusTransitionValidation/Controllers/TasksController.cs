// ============================
// Controllers/TasksController.cs (A-009, A-010)
// ============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly ICurrentUserService _current;
    private readonly ITaskService _tasks;

    public TasksController(ICurrentUserService current, ITaskService tasks)
    {
        _current = current;
        _tasks = tasks;
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] TaskCreateRequest req, CancellationToken ct)
    {
        // A-009: 初期StatusはToDo、status指定は不可（無視） :contentReference[oaicite:17]{index=17}
        var uid = _current.GetRequiredUserId();
        var created = await _tasks.CreateAsync(uid, req, ct);
        return Created($"/api/v1/tasks/{created.TaskId}", created);
    }

    [HttpPut("{taskId:int}")]
    public async Task<ActionResult<TaskResponse>> Update(int taskId, [FromBody] TaskUpdateRequest req, CancellationToken ct)
    {
        // A-010: 状態遷移チェック :contentReference[oaicite:18]{index=18}
        var uid = _current.GetRequiredUserId();
        var updated = await _tasks.UpdateAsync(uid, taskId, req, ct);
        return Ok(updated);
    }
}

