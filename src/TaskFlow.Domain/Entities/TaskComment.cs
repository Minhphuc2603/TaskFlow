using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class TaskComment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid TaskItemId { get; set; }

    // Navigation properties
    public TaskItem TaskItem { get; set; } = null!;
}
