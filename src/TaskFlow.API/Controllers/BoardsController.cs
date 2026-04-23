using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Board;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;
    private readonly IProjectService _projectService;

    public BoardsController(IBoardService boardService, IProjectService projectService)
    {
        _boardService = boardService;
        _projectService = projectService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet("{id}")]
    public async Task<ActionResult<BoardDto>> GetBoard(Guid id)
    {
        var board = await _boardService.GetBoardByIdAsync(id);
        if (board == null) return NotFound();
        return Ok(board);
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<List<BoardDto>>> GetBoardsByProject(Guid projectId)
    {
        var boards = await _boardService.GetBoardsByProjectIdAsync(projectId);
        return Ok(boards);
    }

    [HttpPost("project/{projectId}")]
    public async Task<ActionResult<BoardDto>> CreateBoard(Guid projectId, [FromBody] CreateBoardRequest request)
    {
        var board = await _boardService.CreateBoardAsync(projectId, request.Name);
        return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, board);
    }

    [HttpPut("tasks/{taskId}/move")]
    public async Task<IActionResult> MoveTask(Guid taskId, [FromBody] MoveTaskRequest request)
    {
        try
        {
            await _boardService.MoveTaskAsync(taskId, request.TargetColumnId, request.NewOrder);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("columns/{columnId}/tasks")]
    public async Task<ActionResult<TaskItemDto>> AddTask(Guid columnId, [FromBody] AddTaskRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        try
        {
            var task = await _boardService.AddTaskAsync(columnId, request.Title, request.Description, userId);
            return Ok(task);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("tasks/{taskId}")]
    public async Task<IActionResult> DeleteTask(Guid taskId)
    {
        try
        {
            await _boardService.DeleteTaskAsync(taskId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("tasks/{taskId}")]
    public async Task<ActionResult<TaskItemDto>> UpdateTask(Guid taskId, [FromBody] UpdateTaskRequest request)
    {
        try
        {
            var task = await _boardService.UpdateTaskAsync(taskId, request);
            return Ok(task);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("tasks/{taskId}/comments")]
    public async Task<ActionResult<List<TaskCommentDto>>> GetComments(Guid taskId)
    {
        var comments = await _boardService.GetCommentsAsync(taskId);
        return Ok(comments);
    }

    [HttpPost("tasks/{taskId}/comments")]
    public async Task<ActionResult<TaskCommentDto>> AddComment(Guid taskId, [FromBody] AddCommentRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Unknown";
        if (userId == null) return Unauthorized();

        try
        {
            var comment = await _boardService.AddCommentAsync(taskId, request.Content, userId, userName);
            return Ok(comment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("tasks/{taskId}/comments/{commentId}")]
    public async Task<ActionResult<TaskCommentDto>> UpdateComment(Guid taskId, Guid commentId, [FromBody] UpdateCommentRequest request)
    {
        var userId = GetUserId();
        try
        {
            var comment = await _boardService.UpdateCommentAsync(taskId, commentId, request.Content, userId);
            return Ok(comment);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("tasks/{taskId}/comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(Guid taskId, Guid commentId)
    {
        var userId = GetUserId();
        try
        {
            await _boardService.DeleteCommentAsync(taskId, commentId, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("columns/{columnId}")]
    public async Task<IActionResult> DeleteColumn(Guid columnId)
    {
        // Check ownership via column -> board -> project
        var board = await _boardService.GetBoardByColumnIdAsync(columnId);
        if (board != null)
        {
            var role = await _projectService.GetUserRoleAsync(board.ProjectId, GetUserId());
            if (role != "Owner")
                return StatusCode(403, new { message = "Chỉ chủ sở hữu mới có thể xóa cột." });
        }

        try
        {
            await _boardService.DeleteColumnAsync(columnId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("{boardId}/columns")]
    public async Task<ActionResult<BoardColumnDto>> AddColumn(Guid boardId, [FromBody] AddColumnRequest request)
    {
        // Check ownership via board -> project
        var board = await _boardService.GetBoardByIdAsync(boardId);
        if (board != null)
        {
            var role = await _projectService.GetUserRoleAsync(board.ProjectId, GetUserId());
            if (role != "Owner")
                return StatusCode(403, new { message = "Chỉ chủ sở hữu mới có thể thêm cột." });
        }

        try
        {
            var column = await _boardService.AddColumnAsync(boardId, request.Name, request.Color);
            return Ok(column);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("tasks/{taskId}/labels")]
    public async Task<ActionResult<TaskLabelDto>> AddLabel(Guid taskId, [FromBody] AddTaskLabelRequest request)
    {
        try
        {
            var label = await _boardService.AddTaskLabelAsync(taskId, request.Name, request.Color);
            return Ok(label);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpDelete("tasks/{taskId}/labels/{labelId}")]
    public async Task<IActionResult> DeleteTaskLabel(Guid taskId, Guid labelId)
    {
        try
        {
            await _boardService.DeleteTaskLabelAsync(taskId, labelId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tasks/{taskId}/checklists")]
    public async Task<ActionResult<TaskChecklistDto>> AddChecklist(Guid taskId, [FromBody] AddChecklistRequest request)
    {
        try
        {
            var checklist = await _boardService.AddChecklistAsync(taskId, request.Title);
            return Ok(checklist);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("tasks/{taskId}/checklists/{checklistId}")]
    public async Task<ActionResult<TaskChecklistDto>> UpdateChecklist(Guid taskId, Guid checklistId, [FromBody] UpdateChecklistRequest request)
    {
        try
        {
            var checklist = await _boardService.UpdateChecklistAsync(taskId, checklistId, request.Title, request.IsCompleted);
            return Ok(checklist);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("tasks/{taskId}/checklists/{checklistId}")]
    public async Task<IActionResult> DeleteChecklist(Guid taskId, Guid checklistId)
    {
        try
        {
            await _boardService.DeleteChecklistAsync(taskId, checklistId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPut("{boardId}/columns/{columnId}/move")]
    public async Task<IActionResult> MoveColumn(Guid boardId, Guid columnId, [FromBody] MoveColumRequest request)
    {
        try
        {
            await _boardService.MoveColumnAsync(boardId, columnId, request.NewOrder);
            return Ok();

        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPut("columns/{columnId}")]
    public async Task<ActionResult<BoardColumnDto>> UpdateColumn(Guid columnId, [FromBody] UpdateColumnRequest request)
    {
        var board = await _boardService.GetBoardByColumnIdAsync(columnId);
        if (board != null)
        {
            var role = await _projectService.GetUserRoleAsync(board.ProjectId, GetUserId());
            if (role != "Owner")
                return StatusCode(403, new { message = "Chỉ chủ sở hữu mới có thể sửa tên cột." });
        }
        try
        {
            var column = await _boardService.UpdateColumnAsync(columnId, request.Name, request.Color);
            return Ok(column);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}