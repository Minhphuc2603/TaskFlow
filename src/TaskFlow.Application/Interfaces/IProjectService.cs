using TaskFlow.Application.DTOs.Project;

namespace TaskFlow.Application.Interfaces;

public interface IProjectService
{
    Task<List<ProjectDto>> GetUserProjectsAsync(string userId);
    Task<ProjectDto?> GetProjectByIdAsync(Guid projectId);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, string userId);
    Task<ProjectDto> UpdateProjectAsync(Guid projectId, CreateProjectDto dto);
    Task DeleteProjectAsync(Guid projectId);
    Task<List<ProjectMemberDto>> GetMembersAsync(Guid projectId);
    Task<ProjectMemberDto> AddMemberAsync(Guid projectId, string email);
    Task RemoveMemberAsync(Guid projectId, Guid memberId);
    Task<List<UserSearchDto>> SearchUsersAsync(string query, Guid projectId);
}

