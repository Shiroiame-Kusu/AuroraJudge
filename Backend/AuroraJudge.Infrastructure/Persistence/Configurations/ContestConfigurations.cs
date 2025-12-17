using AuroraJudge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraJudge.Infrastructure.Persistence.Configurations;

public class ContestConfiguration : IEntityTypeConfiguration<Contest>
{
    public void Configure(EntityTypeBuilder<Contest> builder)
    {
        builder.ToTable("contests");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.StartTime).HasColumnName("start_time");
        builder.Property(e => e.EndTime).HasColumnName("end_time");
        builder.Property(e => e.FreezeTime).HasColumnName("freeze_time");
        builder.Property(e => e.UnfreezeTime).HasColumnName("unfreeze_time");
        
        builder.Property(e => e.Type).HasColumnName("type");
        builder.Property(e => e.Visibility).HasColumnName("visibility");
        builder.Property(e => e.Password).HasColumnName("password").HasMaxLength(100);
        
        builder.Property(e => e.IsRated).HasColumnName("is_rated");
        builder.Property(e => e.RatingFloor).HasColumnName("rating_floor");
        builder.Property(e => e.RatingCeiling).HasColumnName("rating_ceiling");
        builder.Property(e => e.AllowLateSubmission).HasColumnName("allow_late_submission");
        builder.Property(e => e.LateSubmissionPenalty).HasColumnName("late_submission_penalty");
        builder.Property(e => e.ShowRanking).HasColumnName("show_ranking");
        builder.Property(e => e.AllowViewOthersCode).HasColumnName("allow_view_others_code");
        builder.Property(e => e.PublishProblemsAfterEnd).HasColumnName("publish_problems_after_end");
        builder.Property(e => e.MaxParticipants).HasColumnName("max_participants");
        builder.Property(e => e.Rules).HasColumnName("rules");
        
        builder.Property(e => e.CreatorId).HasColumnName("creator_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");
        
        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.EndTime);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Visibility);
        
        builder.HasOne(e => e.Creator)
            .WithMany(e => e.CreatedContests)
            .HasForeignKey(e => e.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContestProblemConfiguration : IEntityTypeConfiguration<ContestProblem>
{
    public void Configure(EntityTypeBuilder<ContestProblem> builder)
    {
        builder.ToTable("contest_problems");
        
        builder.HasKey(e => new { e.ContestId, e.ProblemId });
        builder.Property(e => e.ContestId).HasColumnName("contest_id");
        builder.Property(e => e.ProblemId).HasColumnName("problem_id");
        
        builder.Property(e => e.Label).HasColumnName("label").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Order).HasColumnName("order");
        builder.Property(e => e.Score).HasColumnName("score");
        builder.Property(e => e.Color).HasColumnName("color").HasMaxLength(20);
        builder.Property(e => e.SubmissionCount).HasColumnName("submission_count");
        builder.Property(e => e.AcceptedCount).HasColumnName("accepted_count");
        builder.Property(e => e.FirstAcceptedAt).HasColumnName("first_accepted_at");
        builder.Property(e => e.FirstAcceptedBy).HasColumnName("first_accepted_by");
        
        builder.HasIndex(e => new { e.ContestId, e.Label }).IsUnique();
        builder.HasIndex(e => new { e.ContestId, e.Order });
        
        builder.HasOne(e => e.Contest)
            .WithMany(e => e.ContestProblems)
            .HasForeignKey(e => e.ContestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Problem)
            .WithMany(e => e.ContestProblems)
            .HasForeignKey(e => e.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContestParticipantConfiguration : IEntityTypeConfiguration<ContestParticipant>
{
    public void Configure(EntityTypeBuilder<ContestParticipant> builder)
    {
        builder.ToTable("contest_participants");
        
        builder.HasKey(e => new { e.ContestId, e.UserId });
        builder.Property(e => e.ContestId).HasColumnName("contest_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        
        builder.Property(e => e.RegisteredAt).HasColumnName("registered_at");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.IsVirtual).HasColumnName("is_virtual");
        builder.Property(e => e.VirtualStartTime).HasColumnName("virtual_start_time");
        builder.Property(e => e.Score).HasColumnName("score");
        builder.Property(e => e.Penalty).HasColumnName("penalty");
        builder.Property(e => e.Rank).HasColumnName("rank");
        builder.Property(e => e.RatingChange).HasColumnName("rating_change");
        
        builder.HasIndex(e => new { e.ContestId, e.Score, e.Penalty });
        
        builder.HasOne(e => e.Contest)
            .WithMany(e => e.Participants)
            .HasForeignKey(e => e.ContestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.User)
            .WithMany(e => e.ContestParticipations)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContestAnnouncementConfiguration : IEntityTypeConfiguration<ContestAnnouncement>
{
    public void Configure(EntityTypeBuilder<ContestAnnouncement> builder)
    {
        builder.ToTable("contest_announcements");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.ContestId).HasColumnName("contest_id");
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).HasColumnName("content").IsRequired();
        builder.Property(e => e.IsPinned).HasColumnName("is_pinned");
        builder.Property(e => e.ProblemId).HasColumnName("problem_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        
        builder.HasIndex(e => e.ContestId);
        
        builder.HasOne(e => e.Contest)
            .WithMany(e => e.Announcements)
            .HasForeignKey(e => e.ContestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
