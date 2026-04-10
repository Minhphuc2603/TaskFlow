namespace TaskFlow.Application.DTOs.Project;

public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class AddMemberRequest
{
    public string Email { get; set; } = string.Empty;
}

public class UserSearchDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
