using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class TaskChecklist : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public Guid TaskItemId { get; set; }

    // Navigation property
    public TaskItem TaskItem { get; set; } = null!;
}
