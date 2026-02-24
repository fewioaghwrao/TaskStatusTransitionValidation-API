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
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole(); // ★追加
        var created = await _tasks.CreateAsync(uid, role, req, ct); // ★roleを渡す
        return Created($"/api/v1/tasks/{created.TaskId}", created);
    }

    [HttpPut("{taskId:int}")]
    public async Task<ActionResult<TaskResponse>> Update(int taskId, [FromBody] TaskUpdateRequest req, CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole(); // ★追加
        return Ok(await _tasks.UpdateAsync(uid, role, taskId, req, ct)); // ★roleを渡す
    }

    [HttpGet("{taskId:int}")]
    public async Task<ActionResult<TaskResponse>> Get(int taskId, CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole();
        return Ok(await _tasks.GetAsync(uid, role, taskId, ct));
    }
}
