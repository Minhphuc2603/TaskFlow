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

    public BoardsController(IBoardService boardService)
    {
        _boardService = boardService;
    }

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

    [HttpDelete("columns/{columnId}")]
    public async Task<IActionResult> DeleteColumn(Guid columnId)
    {
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
}

public class AddColumnRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class AddTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateBoardRequest
{
    public string Name { get; set; } = string.Empty;
}

public class MoveTaskRequest
{
    public Guid TargetColumnId { get; set; }
    public int NewOrder { get; set; }
}
