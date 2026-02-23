// ============================
// Controllers/ProjectsController.cs (A-004..A-007)
// ============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskStatusTransitionValidation.Contracts;
using TaskStatusTransitionValidation.Services;

namespace TaskStatusTransitionValidation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly ICurrentUserService _current;
    private readonly IProjectService _projects;
    private readonly ITaskService _tasks;

    public ProjectsController(ICurrentUserService current, IProjectService projects, ITaskService tasks)
    {
        _current = current;
        _projects = projects;
        _tasks = tasks;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] bool? archived,
        CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole();
        return Ok(await _projects.ListAsync(uid, role, q, archived, ct));
    }

    [HttpPost]
    [Authorize(Roles = "Leader")]
    public async Task<ActionResult<ProjectDetailResponse>> Create([FromBody] ProjectCreateRequest req, CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var created = await _projects.CreateAsync(uid, req, ct);
        return Created($"/api/v1/projects/{created.ProjectId}", created);
    }

    [HttpGet("{projectId:int}")]
    public async Task<ActionResult<ProjectDetailResponse>> Get(int projectId, CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole();
        return Ok(await _projects.GetAsync(uid, role, projectId, ct));
    }

    [HttpPut("{projectId:int}")]
    public async Task<ActionResult<ProjectDetailResponse>> Update(int projectId, [FromBody] ProjectUpdateRequest req, CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole();
        return Ok(await _projects.UpdateAsync(uid, role, projectId, req, ct));
    }

    [HttpGet("{projectId:int}/tasks")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> ListTasks(int projectId, CancellationToken ct)
    {
        var uid = _current.GetRequiredUserId();
        var role = _current.GetRequiredUserRole();
        return Ok(await _tasks.ListByProjectAsync(uid, role, projectId, ct));
    }
}

