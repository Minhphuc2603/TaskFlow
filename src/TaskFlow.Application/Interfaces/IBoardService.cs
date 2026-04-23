using TaskFlow.Application.DTOs.Board;

namespace TaskFlow.Application.Interfaces;

public interface IBoardService
{
    Task<BoardDto?> GetBoardByIdAsync(Guid boardId);
    Task<List<BoardDto>> GetBoardsByProjectIdAsync(Guid projectId);
    Task<BoardDto> CreateBoardAsync(Guid projectId, string name);
    Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int newOrder);
    Task<TaskItemDto> AddTaskAsync(Guid columnId, string title, string? description, string userId);
    Task DeleteTaskAsync(Guid taskId);
    Task<TaskItemDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request);
    Task<List<TaskCommentDto>> GetCommentsAsync(Guid taskId);
    Task<TaskCommentDto> AddCommentAsync(Guid taskId, string content, string userId, string userName);
    Task<TaskCommentDto> UpdateCommentAsync(Guid taskId, Guid commentId, string content, string userId);
    Task DeleteCommentAsync(Guid taskId, Guid commentId, string userId);
    Task DeleteColumnAsync(Guid columnId);
    Task<BoardColumnDto> AddColumnAsync(Guid boardId, string name, string color);
    Task<BoardDto?> GetBoardByColumnIdAsync(Guid columnId);
    Task<TaskLabelDto> AddTaskLabelAsync(Guid taskId, string name, string color);
    Task DeleteTaskLabelAsync(Guid taskId, Guid labelId);
    Task<TaskChecklistDto> AddChecklistAsync(Guid taskId, string title);
    Task<TaskChecklistDto> UpdateChecklistAsync(Guid taskId, Guid checklistId, string title, bool isCompleted);
    Task DeleteChecklistAsync(Guid taskId, Guid checklistId);
    Task MoveColumnAsync(Guid boardId, Guid columnId, int newOrder);
    Task<BoardColumnDto> UpdateColumnAsync(Guid columnId, string name, string color);
}
