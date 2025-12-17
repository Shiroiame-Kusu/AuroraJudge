using AuroraJudge.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuroraJudge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册应用服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IContestService, ContestService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IRankingService, RankingService>();
        
        return services;
    }
}
