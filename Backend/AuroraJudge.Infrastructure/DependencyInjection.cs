using System.Text;
using AuroraJudge.Application.Services;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using AuroraJudge.Infrastructure.Repositories;
using AuroraJudge.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuroraJudge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 数据库
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });
        
        // 缓存配置 - Redis 可选，无配置时使用内存缓存
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "AuroraJudge:";
            });
        }
        else
        {
            // 使用内存缓存作为后备
            services.AddDistributedMemoryCache();
        }
        
        // JWT 认证
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                ClockSkew = TimeSpan.Zero
            };
            
            // 支持从 Query 参数获取 token（用于 WebSocket）
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
        
        // 授权
        services.AddAuthorization();
        
        // 仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IContestRepository, ContestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // 服务
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IStorageService, StorageService>();
        
        // Judger 调度服务 - 管理评测节点和任务分发
        // 提供 HTTP API 供 Judger 节点连接、拉取任务、上报结果
        services.AddSingleton<IJudgerDispatchService, JudgerDispatchService>();
        
        // 数据库初始化
        services.AddScoped<IDbInitializer, DbInitializer>();
        
        return services;
    }
}
