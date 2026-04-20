using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<TaskChecklist> TaskChecklists => Set<TaskChecklist>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(2000);
        });

        // ProjectMember
        builder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(pm => pm.Id);
            entity.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();

            entity.HasOne(pm => pm.Project)
                  .WithMany(p => p.Members)
                  .HasForeignKey(pm => pm.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Board
        builder.Entity<Board>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);

            entity.HasOne(b => b.Project)
                  .WithMany(p => p.Boards)
                  .HasForeignKey(b => b.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BoardColumn
        builder.Entity<BoardColumn>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);

            entity.HasOne(c => c.Board)
                  .WithMany(b => b.Columns)
                  .HasForeignKey(c => c.BoardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskItem
        builder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(500);
            entity.Property(t => t.Description).HasMaxLength(4000);

            entity.HasOne(t => t.Column)
                  .WithMany(c => c.Tasks)
                  .HasForeignKey(t => t.ColumnId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskComment
        builder.Entity<TaskComment>(entity =>
        {
            entity.HasKey(tc => tc.Id);
            entity.Property(tc => tc.Content).IsRequired().HasMaxLength(2000);

            entity.HasOne(tc => tc.TaskItem)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(tc => tc.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskLabel
        builder.Entity<TaskLabel>(entity =>
        {
            entity.HasKey(tl => tl.Id);
            entity.Property(tl => tl.Name).IsRequired().HasMaxLength(100);
            entity.Property(tl => tl.Color).IsRequired().HasMaxLength(20);

            entity.HasOne(tl => tl.TaskItem)
                  .WithMany(t => t.Labels)
                  .HasForeignKey(tl => tl.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskChecklist
        builder.Entity<TaskChecklist>(entity =>
        {
            entity.HasKey(tc => tc.Id);
            entity.Property(tc => tc.Title).IsRequired().HasMaxLength(200);

            entity.HasOne(tc => tc.TaskItem)
                  .WithMany(t => t.Checklists)
                  .HasForeignKey(tc => tc.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-set UpdatedAt on modified entities
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
