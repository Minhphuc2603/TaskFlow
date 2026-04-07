using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public Priority Priority { get; set; } = Priority.None;
    public DateTime? DueDate { get; set; }
    public string? AssigneeId { get; set; }
    public Guid ColumnId { get; set; }

    // Navigation properties
    public BoardColumn Column { get; set; } = null!;
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskLabel> Labels { get; set; } = new List<TaskLabel>();
}
