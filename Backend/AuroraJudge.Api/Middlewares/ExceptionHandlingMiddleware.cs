using System.Net;
using System.Text.Json;
using AuroraJudge.Domain.Common;
using AuroraJudge.Shared.Models;

namespace AuroraJudge.Api.Middlewares;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "未授权访问"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", "资源不存在"),
            NotFoundException notFoundEx => (HttpStatusCode.NotFound, "NOT_FOUND", notFoundEx.Message),
            ArgumentException => (HttpStatusCode.BadRequest, "BAD_REQUEST", exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, "INVALID_OPERATION", exception.Message),
            ValidationException validationEx => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationEx.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, "FORBIDDEN", "无权执行此操作"),
            ConflictException => (HttpStatusCode.Conflict, "CONFLICT", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "服务器内部错误")
        };

        // Avoid noisy 'unhandled' error logs for expected business/validation failures.
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request failed with {StatusCode} ({ErrorCode}): {Message}", (int)statusCode, errorCode, message);
        }
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        
        var response = ApiResponse.Fail(message, errorCode);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
