using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class TaskLabel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1"; // Default indigo color
    public Guid TaskItemId { get; set; }

    // Navigation properties
    public TaskItem TaskItem { get; set; } = null!;
}
