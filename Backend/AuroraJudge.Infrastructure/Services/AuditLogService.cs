using System.Text.Json;
using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using AuroraJudge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuroraJudge.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    
    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task LogAsync(
        Guid? userId,
        string? username,
        AuditAction action,
        string description,
        string? entityType = null,
        string? entityId = null,
        object? oldValue = null,
        object? newValue = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            Action = action,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };
        
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? userId, string? action, DateTime? startTime, DateTime? endTime,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();
        
        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }
        
        if (!string.IsNullOrEmpty(action) && Enum.TryParse<AuditAction>(action, out var auditAction))
        {
            query = query.Where(l => l.Action == auditAction);
        }
        
        if (startTime.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startTime.Value);
        }
        
        if (endTime.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endTime.Value);
        }
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }
}
