using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TaskFlow.Application.DTOs.Board;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Services;

public class BoardService(ApplicationDbContext context) : IBoardService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<BoardDto?> GetBoardByIdAsync(Guid boardId)
    {
        var board = await _context.Boards
            .Include(b => b.Project)
            .Include(b => b.Columns.OrderBy(c => c.Order))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.Order))
                    .ThenInclude(t => t.Labels)
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
                    .ThenInclude(t => t.Comments)
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
                    .ThenInclude(t => t.Checklists.OrderBy(ch => ch.CreatedAt))
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board == null) return null;

        var assigneeMap = await BuildAssigneeMapAsync(board);
        return MapToDto(board, assigneeMap);
    }

    public async Task<List<BoardDto>> GetBoardsByProjectIdAsync(Guid projectId)
    {
        var boards = await _context.Boards
            .Include(b => b.Project)
            .Where(b => b.ProjectId == projectId)
            .Include(b => b.Columns.OrderBy(c => c.Order))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.Order))
                    .ThenInclude(t => t.Labels)
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
                    .ThenInclude(t => t.Comments)
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
                    .ThenInclude(t => t.Checklists.OrderBy(ch => ch.CreatedAt))
            .ToListAsync();

        // Batch load all assignee names across all boards
        var allAssigneeIds = boards
            .SelectMany(b => b.Columns)
            .SelectMany(c => c.Tasks)
            .Where(t => t.AssigneeId != null)
            .Select(t => t.AssigneeId!)
            .Distinct()
            .ToList();

        var assigneeMap = allAssigneeIds.Count > 0
            ? await _context.Users
                .Where(u => allAssigneeIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName)
            : new Dictionary<string, string>();

        return [.. boards.Select(b => MapToDto(b, assigneeMap))];
    }

    public async Task<BoardDto> CreateBoardAsync(Guid projectId, string name)
    {
        var board = new Board
        {
            Name = name,
            ProjectId = projectId
        };

        _context.Boards.Add(board);

        // Create default columns
        var defaultColumns = new[] 
        { 
            new { Name = "To Do", Color = "#64748b" }, 
            new { Name = "In Progress", Color = "#3b82f6" }, 
            new { Name = "Review", Color = "#fbbf24" }, 
            new { Name = "Done", Color = "#22c55e" } 
        };
        for (int i = 0; i < defaultColumns.Length; i++)
        {
            _context.BoardColumns.Add(new BoardColumn
            {
                Name = defaultColumns[i].Name,
                Color = defaultColumns[i].Color,
                Order = i,
                BoardId = board.Id
            });
        }

        await _context.SaveChangesAsync();

        return (await GetBoardByIdAsync(board.Id))!;
    }

    public async Task MoveTaskAsync(Guid taskId, Guid targetColumnId, int newOrder)
    {
        var task = await _context.TaskItems.FindAsync(taskId);
        if (task == null) throw new Exception("Task not found.");

        var oldColumnId = task.ColumnId;

        // Reorder tasks in the old column
        if (oldColumnId != targetColumnId)
        {
            var oldColumnTasks = await _context.TaskItems
                .Where(t => t.ColumnId == oldColumnId && t.Id != taskId)
                .OrderBy(t => t.Order)
                .ToListAsync();

            for (int i = 0; i < oldColumnTasks.Count; i++)
            {
                oldColumnTasks[i].Order = i;
            }
        }

        // Insert task into target column at the new order
        var targetColumnTasks = await _context.TaskItems
            .Where(t => t.ColumnId == targetColumnId && t.Id != taskId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        task.ColumnId = targetColumnId;
        task.Order = newOrder;

        // Shift tasks after the insertion point
        foreach (var t in targetColumnTasks.Where(t => t.Order >= newOrder))
        {
            t.Order++;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<TaskItemDto> AddTaskAsync(Guid columnId, string title, string? description, string userId)
    {
        var column = await _context.BoardColumns
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == columnId);

        if (column == null) throw new Exception("Column not found.");

        var newOrder = column.Tasks.Count != 0 ? column.Tasks.Max(t => t.Order) + 1 : 0;

        var task = new TaskItem
        {
            Title = title,
            Description = description,
            ColumnId = columnId,
            Order = newOrder,
            CreatedBy = userId,
            Priority = TaskFlow.Domain.Enums.Priority.None
        };

        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();

        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Order = task.Order,
            Priority = task.Priority,
            ColumnId = task.ColumnId,
            CommentCount = 0,
            Labels = [],
            Checklists = []
        };
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await _context.TaskItems.FindAsync(taskId);
        if (task == null) throw new Exception("Task not found.");

        var columnId = task.ColumnId;
        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();

        // Reorder remaining tasks in the column
        var remainingTasks = await _context.TaskItems
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        for (int i = 0; i < remainingTasks.Count; i++)
        {
            remainingTasks[i].Order = i;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<TaskItemDto> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request)
    {
        var task = await _context.TaskItems
            .Include(t => t.Labels)
            .Include(t => t.Comments)
            .Include(t => t.Checklists.OrderBy(ch => ch.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) throw new Exception("Task not found.");

        if (request.Title != null) task.Title = request.Title;
        if (request.Description != null) task.Description = request.Description;
        if (request.Priority.HasValue) task.Priority = request.Priority.Value;
        if (request.ClearDueDate) task.DueDate = null;
        else if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;
        if (request.ClearAssignee) task.AssigneeId = null;
        else if (request.AssigneeId != null) task.AssigneeId = request.AssigneeId;

        await _context.SaveChangesAsync();

        // Resolve assignee name
        string? assigneeName = null;
        if (task.AssigneeId != null)
        {
            assigneeName = await _context.Users
                .Where(u => u.Id == task.AssigneeId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
        }

        return new TaskItemDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Order = task.Order,
            Priority = task.Priority,
            DueDate = task.DueDate,
            AssigneeId = task.AssigneeId,
            AssigneeName = assigneeName,
            ColumnId = task.ColumnId,
            CommentCount = task.Comments.Count,
            Labels = [.. task.Labels.Select(l => new TaskLabelDto
            {
                Id = l.Id,
                Name = l.Name,
                Color = l.Color
            })]
        };
    }

    public async Task<List<TaskCommentDto>> GetCommentsAsync(Guid taskId)
    {
        var comments = await _context.TaskComments
            .Where(c => c.TaskItemId == taskId)
            .Join(_context.Users, 
                  c => c.UserId, 
                  u => u.Id, 
                  (c, u) => new TaskCommentDto
                  {
                      Id = c.Id,
                      Content = c.Content,
                      UserId = c.UserId,
                      UserName = u.FullName ?? "Unknown",
                      CreatedAt = c.CreatedAt
                  })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments;
    }

    public async Task<TaskCommentDto> AddCommentAsync(Guid taskId, string content, string userId, string userName)
    {
        var task = await _context.TaskItems.FindAsync(taskId);
        if (task == null) throw new Exception("Task not found.");

        var comment = new TaskComment
        {
            Content = content,
            UserId = userId,
            TaskItemId = taskId
        };

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();

        return new TaskCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            UserId = comment.UserId,
            UserName = userName,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<TaskCommentDto> UpdateCommentAsync(Guid taskId, Guid commentId, string content, string userId)
    {
        var comment = await _context.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskItemId == taskId)
            ?? throw new Exception("Comment not found.");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bình luận này.");

        comment.Content = content;
        await _context.SaveChangesAsync();

        var userName = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? "Unknown";

        return new TaskCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            UserId = comment.UserId,
            UserName = userName,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task DeleteCommentAsync(Guid taskId, Guid commentId, string userId)
    {
        var comment = await _context.TaskComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskItemId == taskId)
            ?? throw new Exception("Comment not found.");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");

        _context.TaskComments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<BoardDto?> GetBoardByColumnIdAsync(Guid columnId)
    {
        var column = await _context.BoardColumns
            .Include(c => c.Board)
            .FirstOrDefaultAsync(c => c.Id == columnId);
        if (column?.Board == null) return null;

        return new BoardDto
        {
            Id = column.Board.Id,
            ProjectId = column.Board.ProjectId,
            Name = column.Board.Name,
            Columns = []
        };
    }

    private async Task<Dictionary<string, string>> BuildAssigneeMapAsync(Board board)
    {
        var assigneeIds = board.Columns
            .SelectMany(c => c.Tasks)
            .Where(t => t.AssigneeId != null)
            .Select(t => t.AssigneeId!)
            .Distinct()
            .ToList();

        if (assigneeIds.Count == 0) return [];

        return await _context.Users
            .Where(u => assigneeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
    }

    private static BoardDto MapToDto(Board board, Dictionary<string, string> assigneeMap)
    {
        return new BoardDto
        {
            Id = board.Id,
            Name = board.Name,
            ProjectId = board.ProjectId,
            ProjectName = board.Project?.Name ?? string.Empty,
            Columns = [.. board.Columns.Select(c => new BoardColumnDto
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                Order = c.Order,
                Tasks = [.. c.Tasks.Select(t => new TaskItemDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Order = t.Order,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.AssigneeId != null && assigneeMap.TryGetValue(t.AssigneeId, out var name) ? name : null,
                    ColumnId = t.ColumnId,
                    CommentCount = t.Comments.Count,
                    Labels = [.. t.Labels.Select(l => new TaskLabelDto
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Color = l.Color
                    })],
                    Checklists = [.. t.Checklists.OrderBy(ch => ch.CreatedAt).Select(ch => new TaskChecklistDto
                    {
                        Id = ch.Id,
                        Title = ch.Title,
                        IsCompleted = ch.IsCompleted
                    })]
                })]
            })]
        };
    }

    public async Task DeleteColumnAsync(Guid columnId)
    {

        var column = await _context.BoardColumns
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == columnId) ?? throw new Exception("Column not found");

        // Có thể kiểm tra thêm nếu cột còn Task thì không cho xóa, 
        // hoặc xóa luôn cả Task (tùy thuộc vào yêu cầu của bạn vì DeleteBehavior.Cascade đã được set sẵn)

        _context.BoardColumns.Remove(column);
        await _context.SaveChangesAsync();
    }

    public async Task<BoardColumnDto> AddColumnAsync(Guid boardId, string name, string color)
    {
        var board = await _context.Boards.Include(b => b.Columns).FirstOrDefaultAsync(b => b.Id == boardId);
        if (board == null) throw new Exception("Board not found.");

        var newOrder = board.Columns.Count != 0 ? board.Columns.Max(c => c.Order) + 1 : 0;

        var column = new BoardColumn
        {
            Name = name,
            Color = color,
            Order = newOrder,
            BoardId = boardId
        };

        _context.BoardColumns.Add(column);
        await _context.SaveChangesAsync();

        return new BoardColumnDto
        {
            Id = column.Id,
            Name = column.Name,
            Color = column.Color,
            Order = column.Order,
            Tasks = []
        };
    }

    public async Task<TaskLabelDto> AddTaskLabelAsync(Guid taskId, string name, string color)
    {
        //Kiểm tra task tồn tại hay ko ?
        _ = await _context.TaskItems.FindAsync(taskId) ?? throw new Exception("Task not found.");
        //Tạo một nhãn mới 
        var label = new TaskLabel
        {
            Name = name,
            Color = color,
            TaskItemId = taskId
        };
        _context.TaskLabels.Add(label);
        await _context.SaveChangesAsync();
        return new TaskLabelDto
        {
            Id = label.Id,
            Name = label.Name,
            Color = label.Color
        };
    }   

    public async  Task DeleteTaskLabelAsync(Guid taskId, Guid labelId)
    {
        var label = await _context.TaskLabels.FirstOrDefaultAsync(l => l.Id == labelId && l.TaskItemId == taskId) ?? throw new Exception("Label not found.");
        _context.TaskLabels.Remove(label);
        await _context.SaveChangesAsync();
    }

    public async Task<TaskChecklistDto> AddChecklistAsync(Guid taskId, string title)
    {
        _ = await _context.TaskItems.FindAsync(taskId) ?? throw new Exception("Task not found.");
        
        var checklist = new TaskChecklist
        {
            Title = title,
            IsCompleted = false,
            TaskItemId = taskId
        };
        
        _context.TaskChecklists.Add(checklist);
        await _context.SaveChangesAsync();
        
        return new TaskChecklistDto
        {
            Id = checklist.Id,
            Title = checklist.Title,
            IsCompleted = checklist.IsCompleted
        };
    }

    public async Task<TaskChecklistDto> UpdateChecklistAsync(Guid taskId, Guid checklistId, string title, bool isCompleted)
    {
        var checklist = await _context.TaskChecklists.FirstOrDefaultAsync(c => c.Id == checklistId && c.TaskItemId == taskId) 
            ?? throw new Exception("Checklist not found.");
            
        checklist.Title = title;
        checklist.IsCompleted = isCompleted;
        
        await _context.SaveChangesAsync();
        
        return new TaskChecklistDto
        {
            Id = checklist.Id,
            Title = checklist.Title,
            IsCompleted = checklist.IsCompleted
        };
    }

    public async Task DeleteChecklistAsync(Guid taskId, Guid checklistId)
    {
        var checklist = await _context.TaskChecklists.FirstOrDefaultAsync(c => c.Id == checklistId && c.TaskItemId == taskId) 
            ?? throw new Exception("Checklist not found.");
            
        _context.TaskChecklists.Remove(checklist);
        await _context.SaveChangesAsync();
    }
    public async Task MoveColumnAsync(Guid boardId, Guid columId, int newOrder)
    {
        var board = await _context.Boards
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.Id == boardId) ?? throw new Exception("Board not found");
        var column = board.Columns.FirstOrDefault(c => c.Id == columId) 
            ?? throw new Exception("Column not found");
        int oldOrder = column.Order;
        if (oldOrder == newOrder) return;
        //Sap xep lai thu tu cac cot 
        var orderedColumns = board.Columns.OrderBy(c => c.Order).ToList();
        orderedColumns.Remove(column);
        //Dam bao newOrder khong vuot qua gioi han mang 
        newOrder = Math.Clamp(newOrder, 0, orderedColumns.Count);
        orderedColumns.Insert(newOrder, column);
        //Cap nhat lai thuoc tinh Order cho khop voi mang moi 
        for ( int i = 0; i < orderedColumns.Count; i++)
        {
            orderedColumns[i].Order = i;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<BoardColumnDto> UpdateColumnAsync(Guid columnId, string name, string color)
    {
        var column = await _context.BoardColumns.FindAsync(columnId)
            ?? throw new Exception("Column not found");
        column.Name = name;
        column.Color = color;
        await _context.SaveChangesAsync();
        return new BoardColumnDto
        {
            Id = column.Id,
            Name = column.Name,
            Color= column.Color,
            Order = column.Order,
            Tasks = []
        };

    }
}
