namespace AuroraJudge.Domain.Common;

/// <summary>
/// 验证异常 - 用于输入验证失败
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]>? Errors { get; }
    
    public ValidationException(string message) : base(message)
    {
    }
    
    public ValidationException(string message, Dictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}

/// <summary>
/// 禁止访问异常 - 用于权限不足
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>
/// 未找到异常 - 用于资源不存在
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
    
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}

/// <summary>
/// 冲突异常 - 用于资源冲突
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>
/// 业务逻辑异常
/// </summary>
public class BusinessException : Exception
{
    public string? ErrorCode { get; }
    
    public BusinessException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}
