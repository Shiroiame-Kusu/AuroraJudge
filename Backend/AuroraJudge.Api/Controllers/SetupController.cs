using System.Text;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Infrastructure.Persistence;
using AuroraJudge.Shared;
using AuroraJudge.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AuroraJudge.Api.Controllers;

/// <summary>
/// 系统初始化设置控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SetupController> _logger;
    private readonly IWebHostEnvironment _environment;

    public SetupController(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SetupController> logger,
        IWebHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 检查系统是否需要初始化设置
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSetupStatus()
    {
        try
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (context != null)
                {
                    var (hasAny, missingTables) = await CheckSetupTablesAsync(context);

                    // 数据库为空（关键表都不存在）
                    if (!hasAny)
                    {
                        return Ok(new SetupStatusResponse
                        {
                            NeedsSetup = true,
                            Message = "系统需要初始化配置",
                            CurrentConfig = GetCurrentConfig()
                        });
                    }

                    // schema 不完整（部分表存在）
                    if (missingTables.Count > 0)
                    {
                        return Ok(new SetupStatusResponse
                        {
                            NeedsSetup = true,
                            Message = $"检测到数据库表结构不完整，为防止覆盖请先清空数据库后再初始化。缺失关键表: {string.Join(", ", missingTables)}",
                            CurrentConfig = GetCurrentConfig()
                        });
                    }

                    // schema 完整时再检查管理员，避免表不存在导致查询异常。
                    var hasAdmin = await context.Users.AnyAsync(u =>
                        u.UserRoles.Any(ur => ur.RoleId == Roles.AdminRoleId));

                    if (hasAdmin)
                    {
                        return Ok(new SetupStatusResponse
                        {
                            NeedsSetup = false,
                            Message = "系统已完成初始化"
                        });
                    }

                    return Ok(new SetupStatusResponse
                    {
                        NeedsSetup = true,
                        Message = "检测到数据库已存在表结构，但未检测到管理员用户。需要初始化创建管理员。",
                        CurrentConfig = GetCurrentConfig()
                    });
                }
            }
            catch
            {
                // 数据库连接失败，需要设置/修正配置
            }

            return Ok(new SetupStatusResponse
            {
                NeedsSetup = true,
                Message = "系统需要初始化配置",
                CurrentConfig = GetCurrentConfig()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查设置状态时出错");
            return Ok(new SetupStatusResponse
            {
                NeedsSetup = true,
                Message = "无法检查系统状态，请进行初始化配置"
            });
        }
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    [HttpPost("test-database")]
    public async Task<IActionResult> TestDatabaseConnection([FromBody] DatabaseConfig config)
    {
        try
        {
            var connectionString = $"Host={config.Host};Port={config.Port};Database={config.Database};Username={config.Username};Password={config.Password}";
            
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // 检查是否可以创建表
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT version()";
            var version = await cmd.ExecuteScalarAsync();
            
            return Ok(new { success = true, message = $"数据库连接成功: {version}" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = $"数据库连接失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 执行系统初始化
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] SetupRequest request)
    {
        // 关键字段校验：避免前端步骤表单未挂载导致字段缺失，从而使用默认值创建意外的 admin 用户。
        if (request.Admin is null
            || string.IsNullOrWhiteSpace(request.Admin.Username)
            || string.IsNullOrWhiteSpace(request.Admin.Email)
            || string.IsNullOrWhiteSpace(request.Admin.Password))
        {
            return BadRequest(new { success = false, message = "管理员信息不完整，请填写用户名/邮箱/密码后重试" });
        }

        if (request.Database is null
            || string.IsNullOrWhiteSpace(request.Database.Host)
            || string.IsNullOrWhiteSpace(request.Database.Database)
            || string.IsNullOrWhiteSpace(request.Database.Username)
            || string.IsNullOrWhiteSpace(request.Database.Password))
        {
            return BadRequest(new { success = false, message = "数据库配置不完整，请填写主机/库名/用户名/密码后重试" });
        }

        // 安全检查：检查目标数据库是否已存在表，避免误覆盖/重复初始化。
        var connectionStringPrecheck = $"Host={request.Database.Host};Port={request.Database.Port};Database={request.Database.Database};Username={request.Database.Username};Password={request.Database.Password}";
        var precheckOptions = new DbContextOptionsBuilder<ApplicationDbContext>();
        precheckOptions.UseNpgsql(connectionStringPrecheck);

        await using (var precheckContext = new ApplicationDbContext(precheckOptions.Options))
        {
            var (hasAny, missingTables) = await CheckSetupTablesAsync(precheckContext);

            // 有任意关键表存在：说明不是空库。
            if (hasAny)
            {
                // 如果关键表都齐全且已有管理员，则视为已初始化。
                if (missingTables.Count == 0)
                {
                    var hasAdmin = await precheckContext.Users.AnyAsync(u =>
                        u.UserRoles.Any(ur => ur.RoleId == Roles.AdminRoleId));

                    if (hasAdmin)
                    {
                        return BadRequest(new { success = false, message = "系统已经初始化，无法重复执行" });
                    }

                    return BadRequest(new SetupResponse
                    {
                        Success = false,
                        Message = "检测到数据库已存在完整表结构，但未检测到管理员用户。为防止覆盖，已拒绝初始化。请清空数据库后重试，或手动创建管理员并刷新状态。"
                    });
                }

                // 关键表部分存在：说明库处于半初始化/脏状态，拒绝继续。
                return BadRequest(new SetupResponse
                {
                    Success = false,
                    Message = $"检测到数据库并非空库（已存在部分表），为防止覆盖已拒绝初始化。缺失关键表: {string.Join(", ", missingTables)}"
                });
            }
        }

        try
        {
            // 1. 保存配置文件
            await SaveConfigurationAsync(request);

            // 2. 初始化数据库
            var connectionString = $"Host={request.Database.Host};Port={request.Database.Port};Database={request.Database.Database};Username={request.Database.Username};Password={request.Database.Password}";
            
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            
            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            
            // 应用迁移（若项目未创建 EF Migrations，则使用 EnsureCreated 直接建表）
            var migrationsAssembly = context.GetService<IMigrationsAssembly>();
            if (migrationsAssembly.Migrations.Any())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }
            
            // 3. 创建权限和角色
            await SeedPermissionsAsync(context);
            await SeedRolesAsync(context);
            await SeedSystemConfigsAsync(context, request);
            await SeedLanguageConfigsAsync(context);
            
            // 4. 创建管理员用户
            await CreateAdminUserAsync(context, request.Admin);
            
            // 5. 创建 Judger（如果配置了）
            if (request.Judger != null && !string.IsNullOrEmpty(request.Judger.Name))
            {
                var judgerInfo = await CreateJudgerAsync(context, request.Judger);
                
                return Ok(new SetupResponse
                {
                    Success = true,
                    Message = "系统初始化成功！",
                    JudgerCredentials = judgerInfo
                });
            }
            
            return Ok(new SetupResponse
            {
                Success = true,
                Message = "系统初始化成功！请重启服务以应用新配置。"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "系统初始化失败");
            return BadRequest(new SetupResponse
            {
                Success = false,
                Message = $"初始化失败: {ex.Message}"
            });
        }
    }

    private SetupCurrentConfig GetCurrentConfig()
    {
        var config = ConfigReader.CreateDefault("backend.conf");
        return new SetupCurrentConfig
        {
            DatabaseHost = config.Get("database", "host", "localhost"),
            DatabasePort = config.GetInt("database", "port", 5432),
            DatabaseName = config.Get("database", "name", "aurorajudge"),
            DatabaseUser = config.Get("database", "user", "postgres"),
            StorageType = config.Get("storage", "type", "Local"),
            StoragePath = config.Get("storage", "local_path", "./data"),
            JudgeMode = config.Get("judge", "mode", "auto")
        };
    }

    private async Task SaveConfigurationAsync(SetupRequest request)
    {
        var configPath = GetConfigFilePath();
        var sb = new StringBuilder();
        
        sb.AppendLine("# =============================================================================");
        sb.AppendLine("# AuroraJudge Backend 配置文件 (由 Setup 向导自动生成)");
        sb.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("# =============================================================================");
        sb.AppendLine();
        
        // 数据库配置
        sb.AppendLine("[database]");
        sb.AppendLine($"host = {request.Database.Host}");
        sb.AppendLine($"port = {request.Database.Port}");
        sb.AppendLine($"name = {request.Database.Database}");
        sb.AppendLine($"user = {request.Database.Username}");
        sb.AppendLine($"password = {request.Database.Password}");
        sb.AppendLine();
        
        // JWT 配置
        sb.AppendLine("[jwt]");
        var jwtSecret = request.Security?.JwtSecret;
        if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
        {
            jwtSecret = GenerateSecureKey(64);
        }
        sb.AppendLine($"secret = {jwtSecret}");
        sb.AppendLine();
        
        // Redis 配置
        sb.AppendLine("[redis]");
        sb.AppendLine($"connection = {request.Redis?.Connection ?? ""}");
        sb.AppendLine();
        
        // RabbitMQ 配置
        sb.AppendLine("[rabbitmq]");
        sb.AppendLine("connection = ");
        sb.AppendLine();
        
        // 评测模式
        sb.AppendLine("[judge]");
        sb.AppendLine($"mode = {request.Judge?.Mode ?? "auto"}");
        sb.AppendLine();
        
        // 存储配置
        sb.AppendLine("[storage]");
        sb.AppendLine($"type = {request.Storage?.Type ?? "Local"}");
        sb.AppendLine($"local_path = {request.Storage?.LocalPath ?? "./data"}");
        sb.AppendLine();
        
        // MinIO 配置
        if (request.Storage?.Type == "Minio" && request.Storage.Minio != null)
        {
            sb.AppendLine("[minio]");
            sb.AppendLine($"endpoint = {request.Storage.Minio.Endpoint}");
            sb.AppendLine($"access_key = {request.Storage.Minio.AccessKey}");
            sb.AppendLine($"secret_key = {request.Storage.Minio.SecretKey}");
            sb.AppendLine($"use_ssl = {request.Storage.Minio.UseSsl.ToString().ToLower()}");
            sb.AppendLine($"bucket = {request.Storage.Minio.Bucket}");
            sb.AppendLine();
        }
        
        // CORS 配置
        sb.AppendLine("[cors]");
        sb.AppendLine($"origins = {request.Cors?.Origins ?? "http://localhost:5173,http://localhost:3000"}");
        sb.AppendLine();
        
        // 服务器配置
        sb.AppendLine("[server]");
        sb.AppendLine($"environment = {request.Server?.Environment ?? "Production"}");
        sb.AppendLine($"urls = {request.Server?.Urls ?? "http://+:5000"}");
        
        await System.IO.File.WriteAllTextAsync(configPath, sb.ToString());
        _logger.LogInformation("配置文件已保存: {Path}", configPath);
    }

    private static readonly string[] SetupRequiredTables =
    [
        // setup seed 依赖的最小关键表集合
        "permissions",
        "roles",
        "role_permissions",
        "users",
        "user_roles",
        "system_configs",
        "language_configs"
    ];

    private async Task<(bool HasAny, List<string> MissingTables)> CheckSetupTablesAsync(ApplicationDbContext context)
    {
        var missing = new List<string>();
        var anyExists = false;

        foreach (var table in SetupRequiredTables)
        {
            var exists = await CheckTableExistsAsync(context, table);
            if (exists)
            {
                anyExists = true;
            }
            else
            {
                missing.Add(table);
            }
        }

        return (anyExists, missing);
    }

    private static async Task<bool> CheckTableExistsAsync(ApplicationDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
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

    private string GetConfigFilePath()
    {
        // 优先查找项目根目录的配置文件
        var searchPaths = new[]
        {
            "backend.conf",
            "../backend.conf",
            "../../backend.conf",
            "../../../backend.conf",
            Path.Combine(AppContext.BaseDirectory, "backend.conf"),
        };

        foreach (var path in searchPaths)
        {
            if (System.IO.File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        // 默认保存到当前目录
        return Path.GetFullPath("backend.conf");
    }

    private static string GenerateSecureKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

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
            
            // 系统管理
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemSettings, Name = "系统配置", Category = "系统管理", Order = 50 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemAuditLog, Name = "审计日志", Category = "系统管理", Order = 51 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemJudger, Name = "评测机管理", Category = "系统管理", Order = 52 },
            new() { Id = Guid.NewGuid(), Code = Permissions.SystemLanguages, Name = "语言配置", Category = "系统管理", Order = 53 },
            
            // 全部权限
            new() { Id = Guid.NewGuid(), Code = Permissions.All, Name = "全部权限", Category = "系统管理", Order = 99 },
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }

    private async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var allPermissions = await context.Permissions.ToListAsync();
        
        // 管理员角色
        var adminRole = new Role
        {
            Id = Roles.AdminRoleId,
            Name = "系统管理员",
            Code = "admin",
            Description = "拥有所有权限的系统管理员",
            IsSystem = true,
            Priority = 100,
            CreatedAt = DateTime.UtcNow
        };
        context.Roles.Add(adminRole);
        await context.SaveChangesAsync();
        
        // 为管理员分配所有权限
        foreach (var permission in allPermissions)
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }
        
        // 普通用户角色
        var userRole = new Role
        {
            Id = Roles.UserRoleId,
            Name = "普通用户",
            Code = "user",
            Description = "普通注册用户",
            IsSystem = true,
            Priority = 10,
            CreatedAt = DateTime.UtcNow
        };
        context.Roles.Add(userRole);
        
        await context.SaveChangesAsync();
    }

    private async Task SeedSystemConfigsAsync(ApplicationDbContext context, SetupRequest request)
    {
        if (await context.SystemConfigs.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var configs = new List<SystemConfig>
        {
            new() { Id = Guid.NewGuid(), Key = "site.name", Value = request.Site?.Name ?? "Aurora Judge", Type = "string", Category = "站点设置", Description = "站点名称", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "site.description", Value = request.Site?.Description ?? "Online Judge System", Type = "string", Category = "站点设置", Description = "站点描述", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "site.footer", Value = request.Site?.Footer ?? "© 2024 Aurora Judge", Type = "string", Category = "站点设置", Description = "页脚信息", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "user.allow_register", Value = (request.Site?.AllowRegister ?? true).ToString().ToLower(), Type = "boolean", Category = "用户设置", Description = "是否允许注册", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "user.default_role", Value = "User", Type = "string", Category = "用户设置", Description = "默认用户角色", IsPublic = false, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "submission.rate_limit", Value = "10", Type = "number", Category = "提交设置", Description = "每分钟提交次数限制", IsPublic = false, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "judge.default_time_limit", Value = "1000", Type = "number", Category = "评测设置", Description = "默认时间限制(ms)", IsPublic = true, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "judge.default_memory_limit", Value = "256", Type = "number", Category = "评测设置", Description = "默认内存限制(MB)", IsPublic = true, UpdatedAt = now },
        };

        context.SystemConfigs.AddRange(configs);
        await context.SaveChangesAsync();
    }

    private async Task SeedLanguageConfigsAsync(ApplicationDbContext context)
    {
        if (await context.LanguageConfigs.AnyAsync()) return;

        var languages = new List<LanguageConfig>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "c",
                Name = "C (GCC 13)",
                SourceFileName = "main.c",
                ExecutableFileName = "main",
                CompileCommand = "gcc -O2 -std=c17 -o {executable} {source} -lm",
                RunCommand = "./{executable}",
                TimeMultiplier = 1.0,
                MemoryMultiplier = 1.0,
                IsEnabled = true,
                Order = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "cpp",
                Name = "C++ (G++ 13)",
                SourceFileName = "main.cpp",
                ExecutableFileName = "main",
                CompileCommand = "g++ -O2 -std=c++20 -o {executable} {source}",
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
                Name = "Java 21",
                SourceFileName = "Main.java",
                ExecutableFileName = "Main.class",
                CompileCommand = "javac {source}",
                RunCommand = "java Main",
                TimeMultiplier = 2.0,
                MemoryMultiplier = 2.0,
                IsEnabled = true,
                Order = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "python",
                Name = "Python 3.12",
                SourceFileName = "main.py",
                ExecutableFileName = null,
                CompileCommand = null,
                RunCommand = "python3 {source}",
                TimeMultiplier = 3.0,
                MemoryMultiplier = 2.0,
                IsEnabled = true,
                Order = 4
            }
        };

        context.LanguageConfigs.AddRange(languages);
        await context.SaveChangesAsync();
    }

    private async Task CreateAdminUserAsync(ApplicationDbContext context, AdminConfig admin)
    {
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = admin.Username,
            Email = admin.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.Password, BCrypt.Net.BCrypt.GenerateSalt(12)),
            DisplayName = admin.DisplayName ?? "Administrator",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = Roles.AdminRoleId,
            AssignedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        _logger.LogInformation("管理员用户已创建: {Username}", admin.Username);
    }

    private async Task<JudgerCredentials> CreateJudgerAsync(ApplicationDbContext context, JudgerConfig judger)
    {
        var secret = GenerateSecureKey(32);
        var hashedSecret = BCrypt.Net.BCrypt.HashPassword(secret, BCrypt.Net.BCrypt.GenerateSalt(10));

        var judgerNode = new JudgerNode
        {
            Id = Guid.NewGuid(),
            Name = judger.Name,
            Description = judger.Description ?? $"Judger created during setup",
            SecretHash = hashedSecret,
            MaxConcurrentTasks = judger.MaxConcurrentTasks ?? 4,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        context.JudgerNodes.Add(judgerNode);
        await context.SaveChangesAsync();

        _logger.LogInformation("Judger 已创建: {Name} (ID: {Id})", judger.Name, judgerNode.Id);

        return new JudgerCredentials
        {
            JudgerId = judgerNode.Id.ToString(),
            Secret = secret,
            Name = judger.Name
        };
    }
}

#region DTOs

public class SetupStatusResponse
{
    public bool NeedsSetup { get; set; }
    public string Message { get; set; } = "";
    public SetupCurrentConfig? CurrentConfig { get; set; }
}

public class SetupCurrentConfig
{
    public string DatabaseHost { get; set; } = "localhost";
    public int DatabasePort { get; set; } = 5432;
    public string DatabaseName { get; set; } = "aurorajudge";
    public string DatabaseUser { get; set; } = "postgres";
    public string StorageType { get; set; } = "Local";
    public string StoragePath { get; set; } = "./data";
    public string JudgeMode { get; set; } = "auto";
}

public class SetupRequest
{
    public required DatabaseConfig Database { get; set; }
    public required AdminConfig Admin { get; set; }
    public JudgerConfig? Judger { get; set; }
    public SecurityConfig? Security { get; set; }
    public RedisConfig? Redis { get; set; }
    public JudgeConfig? Judge { get; set; }
    public StorageConfig? Storage { get; set; }
    public CorsConfig? Cors { get; set; }
    public ServerConfig? Server { get; set; }
    public SiteConfig? Site { get; set; }
}

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "aurorajudge";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "";
}

public class AdminConfig
{
    public string Username { get; set; } = "admin";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? DisplayName { get; set; }
}

public class JudgerConfig
{
    public string Name { get; set; } = "judger-1";
    public string? Description { get; set; }
    public int? MaxConcurrentTasks { get; set; } = 4;
}

public class SecurityConfig
{
    public string? JwtSecret { get; set; }
}

public class RedisConfig
{
    public string? Connection { get; set; }
}

public class JudgeConfig
{
    public string Mode { get; set; } = "auto";
}

public class StorageConfig
{
    public string Type { get; set; } = "Local";
    public string LocalPath { get; set; } = "./data";
    public MinioConfig? Minio { get; set; }
}

public class MinioConfig
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public bool UseSsl { get; set; } = false;
    public string Bucket { get; set; } = "aurorajudge";
}

public class CorsConfig
{
    public string Origins { get; set; } = "http://localhost:5173";
}

public class ServerConfig
{
    public string Environment { get; set; } = "Production";
    public string Urls { get; set; } = "http://+:5000";
}

public class SiteConfig
{
    public string Name { get; set; } = "Aurora Judge";
    public string Description { get; set; } = "Online Judge System";
    public string Footer { get; set; } = "© 2024 Aurora Judge";
    public bool AllowRegister { get; set; } = true;
}

public class SetupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public JudgerCredentials? JudgerCredentials { get; set; }
}

public class JudgerCredentials
{
    public string JudgerId { get; set; } = "";
    public string Secret { get; set; } = "";
    public string Name { get; set; } = "";
}

#endregion
