using AuroraJudge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuroraJudge.Infrastructure.Persistence;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // 用户与权限
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    
    // 题目相关
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProblemTag> ProblemTags => Set<ProblemTag>();
    
    // 提交相关
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<JudgeResult> JudgeResults => Set<JudgeResult>();
    
    // 比赛相关
    public DbSet<Contest> Contests => Set<Contest>();
    public DbSet<ContestProblem> ContestProblems => Set<ContestProblem>();
    public DbSet<ContestParticipant> ContestParticipants => Set<ContestParticipant>();
    public DbSet<ContestAnnouncement> ContestAnnouncements => Set<ContestAnnouncement>();
    
    // 系统相关
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<LanguageConfig> LanguageConfigs => Set<LanguageConfig>();
    public DbSet<JudgerStatus> JudgerStatuses => Set<JudgerStatus>();
    public DbSet<JudgerNode> JudgerNodes => Set<JudgerNode>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<UserSolvedProblem> UserSolvedProblems => Set<UserSolvedProblem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 应用所有配置
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // 全局查询过滤器 - 软删除
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Problem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Contest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Announcement>().HasQueryFilter(e => !e.IsDeleted);
        
        // 为关联实体添加匹配的查询过滤器，避免 EF Core 警告
        // User 相关
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => !e.User.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(e => !e.User.IsDeleted);
        
        // Problem 相关
        modelBuilder.Entity<ProblemTag>().HasQueryFilter(e => !e.Problem.IsDeleted);
        modelBuilder.Entity<TestCase>().HasQueryFilter(e => !e.Problem.IsDeleted);
        modelBuilder.Entity<Submission>().HasQueryFilter(e => !e.Problem.IsDeleted);
        modelBuilder.Entity<UserSolvedProblem>().HasQueryFilter(e => !e.Problem.IsDeleted);
        modelBuilder.Entity<JudgeResult>().HasQueryFilter(e => !e.Submission.Problem.IsDeleted);
        
        // Contest 相关
        modelBuilder.Entity<ContestProblem>().HasQueryFilter(e => !e.Contest.IsDeleted);
        modelBuilder.Entity<ContestParticipant>().HasQueryFilter(e => !e.Contest.IsDeleted);
        modelBuilder.Entity<ContestAnnouncement>().HasQueryFilter(e => !e.Contest.IsDeleted);
    }
}
