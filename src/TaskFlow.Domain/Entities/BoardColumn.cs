using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class BoardColumn : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Order { get; set; }
    public Guid BoardId { get; set; }

    // Navigation properties
    public Board Board { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
