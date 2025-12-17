using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuroraJudge.Infrastructure.Persistence;

public interface IDbInitializer
{
    Task InitializeAsync();
}

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IHostEnvironment _env;
    
    public DbInitializer(ApplicationDbContext context, ILogger<DbInitializer> logger, IHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }
    
    public async Task InitializeAsync()
    {
        try
        {
            // 注意：GetAppliedMigrationsAsync/GetPendingMigrationsAsync 会查询 __EFMigrationsHistory。
            // 当项目没有迁移时，该表不存在会导致 Npgsql 记录 ERROR 级别日志（虽然通常不影响启动）。
            // 这里改为直接从 MigrationsAssembly 判断是否存在迁移，以避免噪声日志。
            var migrationsAssembly = _context.GetService<IMigrationsAssembly>();
            var hasMigrations = migrationsAssembly.Migrations.Any();

            if (hasMigrations)
            {
                await _context.Database.MigrateAsync();
            }
            else
            {
                // 没有迁移文件：检查关键表是否存在（代表 schema 已创建）
                var permissionsTableExists = await CheckTableExistsAsync("permissions");
                if (!permissionsTableExists)
                {
                    if (!_env.IsDevelopment())
                    {
                        throw new InvalidOperationException(
                            "Database schema is missing but no EF migrations were found. " +
                            "Create migrations (dotnet ef migrations add InitialCreate) and apply them, " +
                            "or run in Development to allow EnsureCreated-based initialization.");
                    }

                    _logger.LogInformation("未检测到迁移文件且表不存在，尝试使用 EnsureCreated 创建数据库架构...");
                    await _context.Database.EnsureCreatedAsync();

                    // EnsureCreated 对“已存在但不完整”的数据库可能不会创建表；二次校验后再决定是否重建
                    permissionsTableExists = await CheckTableExistsAsync("permissions");
                    if (!permissionsTableExists)
                    {
                        _logger.LogWarning("EnsureCreated 未能创建所需表，重建数据库（仅 Development）...");
                        await _context.Database.EnsureDeletedAsync();
                        await _context.Database.EnsureCreatedAsync();
                    }
                }
            }
            
            // 种子数据
            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedSystemConfigsAsync();
            await SeedLanguageConfigsAsync();
            // 不再自动创建 admin 用户，改由 Setup 页面处理
            // await SeedAdminUserAsync();
            
            _logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }
    
    private async Task<bool> CheckTableExistsAsync(string tableName)
    {
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = '{tableName}'
                )";
            var result = await command.ExecuteScalarAsync();
            return result is bool b && b;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
    
    private async Task SeedPermissionsAsync()
    {
        if (await _context.Permissions.AnyAsync())
        {
            return;
        }
        
        var permissions = new List<Permission>
        {
            // 用户管理
            new() { Id = Guid.NewGuid(), Code = Permissions.UserView, Name = "查看用户", Category = "用户管理", Order = 1 },
            new() { Id = Guid.NewGuid(), Code = Permissions.UserCreate, Name = "创建用户", Category = "用户管理", Order = 2 },
            new() { Id = Guid.NewGuid(), Code = Permissions.UserEdit, Name = "编辑用户", Category = "用户管理", Order = 3 },
            new() { Id = Guid.NewGuid(), Code = Permissions.UserDelete, Name = "删除用户", Category = "用户管理", Order = 4 },
            new() { Id = Guid.NewGuid(), Code = Permissions.UserBan, Name = "禁用用户", Category = "用户管理", Order = 5 },
            
            // 角色权限
            new() { Id = Guid.NewGuid(), Code = Permissions.RoleView, Name = "查看角色", Category = "角色权限", Order = 10 },
            new() { Id = Guid.NewGuid(), Code = Permissions.RoleCreate, Name = "创建角色", Category = "角色权限", Order = 11 },
            new() { Id = Guid.NewGuid(), Code = Permissions.RoleEdit, Name = "编辑角色", Category = "角色权限", Order = 12 },
            new() { Id = Guid.NewGuid(), Code = Permissions.RoleDelete, Name = "删除角色", Category = "角色权限", Order = 13 },
            new() { Id = Guid.NewGuid(), Code = Permissions.PermissionView, Name = "查看权限", Category = "角色权限", Order = 14 },
            new() { Id = Guid.NewGuid(), Code = Permissions.PermissionAssign, Name = "分配权限", Category = "角色权限", Order = 15 },
            
            // 题目管理
            new() { Id = Guid.NewGuid(), Code = Permissions.ProblemView, Name = "查看题目", Category = "题目管理", Order = 20 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ProblemCreate, Name = "创建题目", Category = "题目管理", Order = 21 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ProblemEdit, Name = "编辑题目", Category = "题目管理", Order = 22 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ProblemDelete, Name = "删除题目", Category = "题目管理", Order = 23 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ProblemManageTestCases, Name = "管理测试用例", Category = "题目管理", Order = 24 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ProblemRejudge, Name = "重判题目", Category = "题目管理", Order = 25 },
            
            // 提交管理
            new() { Id = Guid.NewGuid(), Code = Permissions.SubmissionView, Name = "查看提交", Category = "提交管理", Order = 30 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SubmissionViewCode, Name = "查看代码", Category = "提交管理", Order = 31 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SubmissionRejudge, Name = "重判提交", Category = "提交管理", Order = 32 },
            
            // 比赛管理
            new() { Id = Guid.NewGuid(), Code = Permissions.ContestView, Name = "查看比赛", Category = "比赛管理", Order = 40 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ContestCreate, Name = "创建比赛", Category = "比赛管理", Order = 41 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ContestEdit, Name = "编辑比赛", Category = "比赛管理", Order = 42 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ContestDelete, Name = "删除比赛", Category = "比赛管理", Order = 43 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ContestManageParticipants, Name = "管理参赛者", Category = "比赛管理", Order = 44 },
            new() { Id = Guid.NewGuid(), Code = Permissions.ContestAnnouncement, Name = "发布公告", Category = "比赛管理", Order = 45 },
            
            // 系统管理
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemSettings, Name = "系统设置", Category = "系统管理", Order = 50 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemLanguages, Name = "语言配置", Category = "系统管理", Order = 51 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemJudger, Name = "判题机管理", Category = "系统管理", Order = 52 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemAuditLog, Name = "审计日志", Category = "系统管理", Order = 53 },
            new() { Id = Guid.NewGuid(), Code = Permissions.All, Name = "全部权限", Category = "系统管理", Order = 99 }
        };
        
        _context.Permissions.AddRange(permissions);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync())
        {
            return;
        }
        
        var allPermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Code == Permissions.All);
        var problemPermissions = await _context.Permissions
            .Where(p => p.Code.StartsWith("problem:"))
            .ToListAsync();
        
        var adminRole = new Role
        {
            Id = Roles.AdminRoleId,
            Name = Roles.Admin,
            Code = "admin",
            Description = "系统管理员，拥有全部权限",
            IsSystem = true,
            Priority = 100,
            CreatedAt = DateTime.UtcNow
        };
        
        var problemSetterRole = new Role
        {
            Id = Roles.ProblemSetterRoleId,
            Name = Roles.ProblemSetter,
            Code = "problem_setter",
            Description = "可以创建和管理题目",
            IsSystem = true,
            Priority = 50,
            CreatedAt = DateTime.UtcNow
        };
        
        var userRole = new Role
        {
            Id = Roles.UserRoleId,
            Name = Roles.User,
            Code = "user",
            Description = "普通注册用户",
            IsSystem = true,
            Priority = 10,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Roles.AddRange(adminRole, problemSetterRole, userRole);
        await _context.SaveChangesAsync();
        
        // 分配权限
        if (allPermission != null)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = allPermission.Id
            });
        }
        
        foreach (var permission in problemPermissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = problemSetterRole.Id,
                PermissionId = permission.Id
            });
        }
        
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedSystemConfigsAsync()
    {
        if (await _context.SystemConfigs.AnyAsync())
        {
            return;
        }
        
        var now = DateTime.UtcNow;
        var configs = new List<SystemConfig>
        {
            new() { Id = Guid.NewGuid(), Key = ConfigKeys.SiteName, Value = "Aurora Judge", Description = "站点名称", Type = "string", Category = "站点", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = ConfigKeys.SiteDescription, Value = "一个现代化的在线评测系统", Description = "站点描述", Type = "string", Category = "站点", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = ConfigKeys.AllowRegistration, Value = "true", Description = "是否允许注册", Type = "boolean", Category = "用户", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = ConfigKeys.SubmissionRateLimit, Value = "5", Description = "提交频率限制（秒）", Type = "number", Category = "提交", IsPublic = false, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = ConfigKeys.MaxCodeLength, Value = "65536", Description = "最大代码长度（字节）", Type = "number", Category = "提交", IsPublic = true, UpdatedAt = now }
        };
        
        _context.SystemConfigs.AddRange(configs);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedLanguageConfigsAsync()
    {
        if (await _context.LanguageConfigs.AnyAsync())
        {
            return;
        }
        
        var languages = new List<LanguageConfig>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "cpp",
                Name = "C++ 17",
                SourceFileName = "main.cpp",
                ExecutableFileName = "main",
                CompileCommand = "g++ -std=c++17 -O2 -o {output} {source}",
                RunCommand = "./{executable}",
                TimeMultiplier = 1.0,
                MemoryMultiplier = 1.0,
                IsEnabled = true,
                Order = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "c",
                Name = "C 11",
                SourceFileName = "main.c",
                ExecutableFileName = "main",
                CompileCommand = "gcc -std=c11 -O2 -o {output} {source}",
                RunCommand = "./{executable}",
                TimeMultiplier = 1.0,
                MemoryMultiplier = 1.0,
                IsEnabled = true,
                Order = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "java",
                Name = "Java 17",
                SourceFileName = "Main.java",
                ExecutableFileName = "Main",
                CompileCommand = "javac {source}",
                RunCommand = "java -Xmx{memory}m Main",
                TimeMultiplier = 2.0,
                MemoryMultiplier = 2.0,
                IsEnabled = true,
                Order = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "python",
                Name = "Python 3.11",
                SourceFileName = "main.py",
                ExecutableFileName = null,
                CompileCommand = null,
                RunCommand = "python3 {source}",
                TimeMultiplier = 3.0,
                MemoryMultiplier = 2.0,
                IsEnabled = true,
                Order = 4
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "csharp",
                Name = "C# 12",
                SourceFileName = "Program.cs",
                ExecutableFileName = null,
                CompileCommand = "dotnet build -c Release",
                RunCommand = "dotnet run --no-build",
                TimeMultiplier = 2.0,
                MemoryMultiplier = 2.0,
                IsEnabled = true,
                Order = 5
            }
        };
        
        _context.LanguageConfigs.AddRange(languages);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Username == "admin"))
        {
            return;
        }
        
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@aurorajudge.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", BCrypt.Net.BCrypt.GenerateSalt(12)),
            DisplayName = "Administrator",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();
        
        // 分配管理员角色
        _context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = Roles.AdminRoleId,
            AssignedAt = DateTime.UtcNow
        });
        
        await _context.SaveChangesAsync();
    }
}
