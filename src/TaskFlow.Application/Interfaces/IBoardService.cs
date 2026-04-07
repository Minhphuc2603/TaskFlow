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
}
