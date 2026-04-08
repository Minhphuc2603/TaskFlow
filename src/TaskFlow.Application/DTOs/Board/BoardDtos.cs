using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs.Board;

public class BoardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<BoardColumnDto> Columns { get; set; } = new();
}

public class BoardColumnDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<TaskItemDto> Tasks { get; set; } = new();
}

public class TaskItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public Priority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public Guid ColumnId { get; set; }
    public int CommentCount { get; set; }
    public List<TaskLabelDto> Labels { get; set; } = new();
}

public class TaskLabelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Priority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public bool ClearDueDate { get; set; } = false;
}

public class TaskCommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
