using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Project;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetMyProjects()
    {
        var userId = GetUserId();
        var projects = await _projectService.GetUserProjectsAsync(userId);
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProject(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto)
    {
        var userId = GetUserId();
        var project = await _projectService.CreateProjectAsync(dto, userId);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(Guid id, [FromBody] CreateProjectDto dto)
    {
        try
        {
            var project = await _projectService.UpdateProjectAsync(id, dto);
            return Ok(project);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        try
        {
            await _projectService.DeleteProjectAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<ProjectMemberDto>>> GetMembers(Guid id)
    {
        var members = await _projectService.GetMembersAsync(id);
        return Ok(members);
    }

    [HttpPost("{id}/members")]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        try
        {
            var member = await _projectService.AddMemberAsync(id, request.Email);
            return Ok(member);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        try
        {
            await _projectService.RemoveMemberAsync(id, memberId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("users/search")]
    public async Task<ActionResult<List<UserSearchDto>>> SearchUsers(
        [FromQuery] string q, [FromQuery] Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<UserSearchDto>());

        var users = await _projectService.SearchUsersAsync(q, projectId);
        return Ok(users);
    }
}
