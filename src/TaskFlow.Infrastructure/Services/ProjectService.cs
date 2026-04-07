using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.DTOs.Project;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectDto>> GetUserProjectsAsync(string userId)
    {
        var projects = await _context.ProjectMembers
            .Where(pm => pm.UserId == userId)
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Members)
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Boards)
            .Select(pm => new ProjectDto
            {
                Id = pm.Project.Id,
                Name = pm.Project.Name,
                Description = pm.Project.Description,
                CoverImageUrl = pm.Project.CoverImageUrl,
                CreatedAt = pm.Project.CreatedAt,
                MemberCount = pm.Project.Members.Count,
                BoardCount = pm.Project.Boards.Count
            })
            .ToListAsync();

        return projects;
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(Guid projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Members)
            .Include(p => p.Boards)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null) return null;

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CoverImageUrl = project.CoverImageUrl,
            CreatedAt = project.CreatedAt,
            MemberCount = project.Members.Count,
            BoardCount = project.Boards.Count
        };
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, string userId)
    {
        var project = new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedBy = userId
        };

        _context.Projects.Add(project);

        // Add the creator as Owner
        var member = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRole.Owner,
            CreatedBy = userId
        };

        _context.ProjectMembers.Add(member);

        // Create a default board with default columns
        var board = new Board
        {
            Name = "Main Board",
            ProjectId = project.Id,
            CreatedBy = userId
        };

        _context.Boards.Add(board);

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
                BoardId = board.Id,
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync();

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            MemberCount = 1,
            BoardCount = 1
        };
    }

    public async Task<ProjectDto> UpdateProjectAsync(Guid projectId, CreateProjectDto dto)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) throw new Exception("Project not found.");

        project.Name = dto.Name;
        project.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt
        };
    }

    public async Task DeleteProjectAsync(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) throw new Exception("Project not found.");

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }
}
