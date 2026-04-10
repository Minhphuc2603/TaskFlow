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
                BoardCount = pm.Project.Boards.Count,
                UserRole = pm.Role.ToString()
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

    public async Task<List<ProjectMemberDto>> GetMembersAsync(Guid projectId)
    {
        var members = await _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId)
            .Join(_context.Users,
                  pm => pm.UserId,
                  u => u.Id,
                  (pm, u) => new ProjectMemberDto
                  {
                      Id = pm.Id,
                      UserId = pm.UserId,
                      FullName = u.FullName,
                      Email = u.Email ?? "",
                      Role = pm.Role.ToString(),
                      JoinedAt = pm.CreatedAt
                  })
            .ToListAsync();

        return members;
    }

    public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            throw new Exception("Không tìm thấy người dùng với email này. Họ cần đăng ký tài khoản trước.");

        var existing = await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == user.Id);
        if (existing)
            throw new Exception("Người dùng này đã là thành viên của dự án.");

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = user.Id,
            Role = ProjectRole.Member
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        return new ProjectMemberDto
        {
            Id = member.Id,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            Role = "Member",
            JoinedAt = member.CreatedAt
        };
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid memberId)
    {
        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.Id == memberId && pm.ProjectId == projectId);
        if (member == null) throw new Exception("Thành viên không tồn tại.");
        if (member.Role == ProjectRole.Owner)
            throw new Exception("Không thể xóa chủ sở hữu dự án.");

        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserSearchDto>> SearchUsersAsync(string query, Guid projectId)
    {
        var existingMemberIds = await _context.ProjectMembers
            .Where(pm => pm.ProjectId == projectId)
            .Select(pm => pm.UserId)
            .ToListAsync();

        var users = await _context.Users
            .Where(u => !existingMemberIds.Contains(u.Id) &&
                        (u.Email!.Contains(query) || u.FullName.Contains(query)))
            .Take(10)
            .Select(u => new UserSearchDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? ""
            })
            .ToListAsync();

        return users;
    }

    public async Task<string?> GetUserRoleAsync(Guid projectId, string userId)
    {
        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        return member?.Role.ToString();
    }
}
