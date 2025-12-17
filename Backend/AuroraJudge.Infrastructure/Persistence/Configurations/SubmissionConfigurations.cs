using AuroraJudge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraJudge.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.ProblemId).HasColumnName("problem_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ContestId).HasColumnName("contest_id");
        
        builder.Property(e => e.Code).HasColumnName("code").IsRequired();
        builder.Property(e => e.Language).HasColumnName("language").HasMaxLength(20).IsRequired();
        builder.Property(e => e.CodeLength).HasColumnName("code_length");
        builder.Property(e => e.SubmitIp).HasColumnName("submit_ip").HasMaxLength(50);
        
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.Score).HasColumnName("score");
        builder.Property(e => e.TimeUsed).HasColumnName("time_used");
        builder.Property(e => e.MemoryUsed).HasColumnName("memory_used");
        builder.Property(e => e.CompileMessage).HasColumnName("compile_message");
        builder.Property(e => e.JudgeMessage).HasColumnName("judge_message");
        builder.Property(e => e.JudgedAt).HasColumnName("judged_at");
        builder.Property(e => e.JudgerId).HasColumnName("judger_id").HasMaxLength(100);
        builder.Property(e => e.IsAfterContest).HasColumnName("is_after_contest");
        builder.Property(e => e.ShareStatus).HasColumnName("share_status");
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        
        builder.HasIndex(e => e.ProblemId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ContestId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Language);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.ProblemId, e.UserId, e.Status });
        
        builder.HasOne(e => e.Problem)
            .WithMany(e => e.Submissions)
            .HasForeignKey(e => e.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.User)
            .WithMany(e => e.Submissions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Contest)
            .WithMany(e => e.Submissions)
            .HasForeignKey(e => e.ContestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class JudgeResultConfiguration : IEntityTypeConfiguration<JudgeResult>
{
    public void Configure(EntityTypeBuilder<JudgeResult> builder)
    {
        builder.ToTable("judge_results");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.SubmissionId).HasColumnName("submission_id");
        builder.Property(e => e.TestCaseOrder).HasColumnName("test_case_order");
        builder.Property(e => e.Subtask).HasColumnName("subtask");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.TimeUsed).HasColumnName("time_used");
        builder.Property(e => e.MemoryUsed).HasColumnName("memory_used");
        builder.Property(e => e.Score).HasColumnName("score");
        builder.Property(e => e.ExitCode).HasColumnName("exit_code");
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(1000);
        builder.Property(e => e.CheckerOutput).HasColumnName("checker_output").HasMaxLength(1000);
        
        builder.HasIndex(e => new { e.SubmissionId, e.TestCaseOrder });
        
        builder.HasOne(e => e.Submission)
            .WithMany(e => e.JudgeResults)
            .HasForeignKey(e => e.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
