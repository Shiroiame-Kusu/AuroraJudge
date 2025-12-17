using AuroraJudge.Application;
using AuroraJudge.Infrastructure;
using AuroraJudge.Infrastructure.Persistence;
using AuroraJudge.Api.Middlewares;
using AuroraJudge.Shared;
using AuroraJudge.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

// 加载配置文件
var config = ConfigReader.CreateDefault("backend.conf");
if (config.IsLoaded)
{
    Console.WriteLine($"✓ 已加载配置文件: {config.FilePath}");
}
else
{
    Console.WriteLine("⚠ 未找到 backend.conf 配置文件，使用默认配置");
}

var builder = WebApplication.CreateBuilder(args);

// 将 .conf 配置应用到 Configuration
config.ApplyTo(builder.Configuration);

// 将环境变量添加到配置中 (环境变量优先级更高，可覆盖配置文件)
builder.Configuration.AddEnvironmentVariables();

// 从配置构建连接字符串等
ConfigureFromConfig(builder.Configuration, config);

// 验证必需的配置
ValidateConfiguration(builder.Configuration);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/aurorajudge-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger 配置
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuroraJudge API",
        Version = "v1",
        Description = "Online Judge System API",
        Contact = new OpenApiContact
        {
            Name = "AuroraJudge Team",
            Email = "support@aurorajudge.com"
        }
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 添加应用层服务
builder.Services.AddApplicationServices();

// 添加基础设施层服务
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" }
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// 添加健康检查
builder.Services.AddHealthChecks();

var app = builder.Build();

// 配置中间件管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuroraJudge API v1");
        c.RoutePrefix = "swagger";
    });
}

// 全局异常处理
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 请求日志
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// 数据库迁移和种子数据
using (var scope = app.Services.CreateScope())
{
    // 方案 A：不使用 setup.lock，初始化状态以数据库为准。
    // 启动时仅在“schema 完整 + 管理员存在”时执行 seed/初始化，避免在未 setup 时误建库。
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            static async Task<bool> TableExistsAsync(System.Data.Common.DbConnection conn, string table)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = '{table}'
                    )";
                var res = await cmd.ExecuteScalarAsync();
                return res is bool b && b;
            }

            var schemaReady =
                await TableExistsAsync(connection, "permissions")
                && await TableExistsAsync(connection, "roles")
                && await TableExistsAsync(connection, "users")
                && await TableExistsAsync(connection, "user_roles");

            if (!schemaReady)
            {
                Log.Information("Database schema not ready; skipping automatic database initialization. Run Setup to initialize.");
                // IMPORTANT: don't exit the process; keep the API running so /api/setup/* can be used.
                goto StartupInitDone;
            }

            var hasAdmin = await db.Users.AnyAsync(u =>
                u.UserRoles.Any(ur => ur.RoleId == Roles.AdminRoleId));

            if (!hasAdmin)
            {
                Log.Information("No admin user found; skipping automatic database initialization. Run Setup to create admin.");
                // IMPORTANT: don't exit the process; keep the API running so /api/setup/* can be used.
                goto StartupInitDone;
            }

            var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await initializer.InitializeAsync();

            StartupInitDone: ;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to validate database readiness on startup; skipping automatic database initialization.");
    }
}

Log.Information("AuroraJudge API is starting...");

app.Run();

/// <summary>
/// 从配置文件构建应用程序配置
/// </summary>
static void ConfigureFromConfig(IConfigurationManager configuration, ConfigReader config)
{
    // 构建 PostgreSQL 连接字符串
    var pgHost = config.Get("database", "host", "localhost");
    var pgPort = config.Get("database", "port", "5432");
    var pgDb = config.Get("database", "name", "aurorajudge");
    var pgUser = config.Get("database", "user", "postgres");
    var pgPassword = config.Get("database", "password", "postgres");
    
    var defaultConnection = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPassword}";
    configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
    
    // JWT 密钥
    var jwtSecret = config.Get("jwt", "secret");
    if (!string.IsNullOrEmpty(jwtSecret))
    {
        configuration["Jwt:Key"] = jwtSecret;
    }
    
    // Redis 连接 (可选)
    var redisConnection = config.Get("redis", "connection");
    if (!string.IsNullOrEmpty(redisConnection))
    {
        configuration["ConnectionStrings:Redis"] = redisConnection;
    }
    
    // RabbitMQ 连接 (可选)
    var rabbitMqConnection = config.Get("rabbitmq", "connection");
    if (!string.IsNullOrEmpty(rabbitMqConnection))
    {
        configuration["ConnectionStrings:RabbitMQ"] = rabbitMqConnection;
    }
    
    // 评测模式配置: auto(自动), rabbitmq(强制RabbitMQ), inprocess(内嵌评测)
    var judgeMode = config.Get("judge", "mode", "auto");
    configuration["Judge:Mode"] = judgeMode;
    
    // 存储配置
    var storageType = config.Get("storage", "type", "Local");
    configuration["Storage:Type"] = storageType;
    
    var storagePath = config.Get("storage", "local_path", "./data");
    configuration["Storage:LocalPath"] = storagePath;
    
    // MinIO 配置
    var minioEndpoint = config.Get("minio", "endpoint");
    if (!string.IsNullOrEmpty(minioEndpoint))
    {
        configuration["Storage:Minio:Endpoint"] = minioEndpoint;
        configuration["Storage:Minio:AccessKey"] = config.Get("minio", "access_key");
        configuration["Storage:Minio:SecretKey"] = config.Get("minio", "secret_key");
        configuration["Storage:Minio:UseSSL"] = config.Get("minio", "use_ssl", "false");
        configuration["Storage:Minio:BucketName"] = config.Get("minio", "bucket", "aurorajudge");
    }
    
    // CORS Origins
    var corsOrigins = config.Get("cors", "origins");
    if (!string.IsNullOrEmpty(corsOrigins))
    {
        var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < origins.Length; i++)
        {
            configuration[$"Cors:Origins:{i}"] = origins[i].Trim();
        }
    }
    
    // 服务器配置
    var environment = config.Get("server", "environment");
    if (!string.IsNullOrEmpty(environment))
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
    }
}

/// <summary>
/// 验证必需的配置项
/// </summary>
static void ValidateConfiguration(IConfiguration configuration)
{
    var errors = new List<string>();
    
    // 验证数据库连接
    var dbConnection = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(dbConnection))
    {
        errors.Add("数据库连接字符串未配置 (请检查 backend.conf 中的 [database] 配置)");
    }
    
    // 验证 JWT 密钥
    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        errors.Add("JWT 密钥未配置 (请检查 backend.conf 中的 [jwt] secret)");
    }
    else if (jwtKey.Length < 32)
    {
        errors.Add("JWT 密钥长度必须至少 32 个字符");
    }
    
    // 如果有错误，抛出异常终止启动
    if (errors.Count > 0)
    {
        var message = "配置验证失败:\n" + string.Join("\n", errors.Select(e => $"  - {e}"));
        throw new InvalidOperationException(message);
    }
    
    // 判断评测模式
    var judgeMode = configuration["Judge:Mode"]?.ToLower() ?? "auto";
    var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ");
    var useRabbitMq = judgeMode == "rabbitmq" || (judgeMode == "auto" && !string.IsNullOrEmpty(rabbitMqConnection));
    
    // 记录可选服务状态
    var redisEnabled = !string.IsNullOrEmpty(configuration.GetConnectionString("Redis"));
    var storageType = configuration["Storage:Type"] ?? "Local";
    
    var judgeModeDisplay = useRabbitMq ? "RabbitMQ + Judger ✓" : "内嵌评测 (InProcess)";
    var cacheDisplay = redisEnabled ? "Redis ✓" : "内存缓存";
    
    Console.WriteLine($"""
    ╔════════════════════════════════════════════════════════════════╗
    ║                     AuroraJudge 配置状态                         ║
    ╠════════════════════════════════════════════════════════════════╣
    ║  数据库:       PostgreSQL ✓                                      ║
    ║  缓存:         {cacheDisplay,-20}                               ║
    ║  评测模式:     {judgeModeDisplay,-20}                           ║
    ║  文件存储:     {storageType,-20}                                 ║
    ╠════════════════════════════════════════════════════════════════╣
    ║  切换评测模式: 修改 backend.conf 中的 [judge] mode 后重启          ║
    ║    - auto: 自动检测 (有 RabbitMQ 用 RabbitMQ，否则用内嵌)           ║
    ║    - rabbitmq: 强制使用 RabbitMQ + Judger                        ║
    ║    - inprocess: 强制使用内嵌评测                                  ║
    ╚════════════════════════════════════════════════════════════════╝
    """);
}
