using AuroraJudge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraJudge.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.IsPinned).HasColumnName("is_pinned");
        builder.Property(e => e.PinOrder).HasColumnName("pin_order");
        builder.Property(e => e.PublishedAt).HasColumnName("published_at");
        builder.Property(e => e.ViewCount).HasColumnName("view_count");
        builder.Property(e => e.AuthorId).HasColumnName("author_id");
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");
        
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.IsPinned, e.PinOrder });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
        builder.Property(e => e.Action).HasColumnName("action");
        builder.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.OldValue).HasColumnName("old_value");
        builder.Property(e => e.NewValue).HasColumnName("new_value");
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(e => e.Timestamp).HasColumnName("timestamp");
        builder.Property(e => e.ExtraData).HasColumnName("extra_data");
        
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
    }
}

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();
        
        builder.Property(e => e.Value).HasColumnName("value").IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.IsPublic).HasColumnName("is_public");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        
        builder.HasIndex(e => e.Category);
    }
}

public class LanguageConfigConfiguration : IEntityTypeConfiguration<LanguageConfig>
{
    public void Configure(EntityTypeBuilder<LanguageConfig> builder)
    {
        builder.ToTable("language_configs");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
        
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(50);
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled");
        builder.Property(e => e.CompileCommand).HasColumnName("compile_command").HasMaxLength(1000);
        builder.Property(e => e.RunCommand).HasColumnName("run_command").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.SourceFileName).HasColumnName("source_file_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ExecutableFileName).HasColumnName("executable_file_name").HasMaxLength(100);
        builder.Property(e => e.CompileTimeLimit).HasColumnName("compile_time_limit");
        builder.Property(e => e.CompileMemoryLimit).HasColumnName("compile_memory_limit");
        builder.Property(e => e.TimeMultiplier).HasColumnName("time_multiplier");
        builder.Property(e => e.MemoryMultiplier).HasColumnName("memory_multiplier");
        builder.Property(e => e.MonacoLanguage).HasColumnName("monaco_language").HasMaxLength(50);
        builder.Property(e => e.Template).HasColumnName("template");
        builder.Property(e => e.Order).HasColumnName("order");
    }
}

public class JudgerStatusConfiguration : IEntityTypeConfiguration<JudgerStatus>
{
    public void Configure(EntityTypeBuilder<JudgerStatus> builder)
    {
        builder.ToTable("judger_statuses");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.JudgerId).HasColumnName("judger_id").HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.JudgerId).IsUnique();
        
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Hostname).HasColumnName("hostname").HasMaxLength(200);
        builder.Property(e => e.IsOnline).HasColumnName("is_online");
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled");
        builder.Property(e => e.CurrentTasks).HasColumnName("current_tasks");
        builder.Property(e => e.MaxTasks).HasColumnName("max_tasks");
        builder.Property(e => e.CpuUsage).HasColumnName("cpu_usage");
        builder.Property(e => e.MemoryUsage).HasColumnName("memory_usage");
        builder.Property(e => e.CompletedTasks).HasColumnName("completed_tasks");
        builder.Property(e => e.Version).HasColumnName("version").HasMaxLength(50);
        builder.Property(e => e.LastHeartbeat).HasColumnName("last_heartbeat");
        builder.Property(e => e.StartedAt).HasColumnName("started_at");
    }
}

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
        
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled");
        builder.Property(e => e.Conditions).HasColumnName("conditions");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}

public class JudgerNodeConfiguration : IEntityTypeConfiguration<JudgerNode>
{
    public void Configure(EntityTypeBuilder<JudgerNode> builder)
    {
        builder.ToTable("judger_nodes");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Name).IsUnique();
        
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.SecretHash).HasColumnName("secret_hash").HasMaxLength(200).IsRequired();
        builder.Property(e => e.MaxConcurrentTasks).HasColumnName("max_concurrent_tasks");
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled");
        builder.Property(e => e.SupportedLanguages).HasColumnName("supported_languages").HasMaxLength(500);
        builder.Property(e => e.LastConnectedAt).HasColumnName("last_connected_at");
        builder.Property(e => e.LastConnectedIp).HasColumnName("last_connected_ip").HasMaxLength(50);
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");
    }
}
