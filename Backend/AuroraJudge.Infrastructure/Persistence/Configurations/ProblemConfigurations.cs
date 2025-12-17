using AuroraJudge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuroraJudge.Infrastructure.Persistence.Configurations;

public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.ToTable("problems");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.InputFormat).HasColumnName("input_format").IsRequired();
        builder.Property(e => e.OutputFormat).HasColumnName("output_format").IsRequired();
        builder.Property(e => e.SampleInput).HasColumnName("sample_input");
        builder.Property(e => e.SampleOutput).HasColumnName("sample_output");
        builder.Property(e => e.Hint).HasColumnName("hint");
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(200);
        
        builder.Property(e => e.TimeLimit).HasColumnName("time_limit").HasDefaultValue(1000);
        builder.Property(e => e.MemoryLimit).HasColumnName("memory_limit").HasDefaultValue(262144);
        builder.Property(e => e.StackLimit).HasColumnName("stack_limit").HasDefaultValue(65536);
        builder.Property(e => e.OutputLimit).HasColumnName("output_limit").HasDefaultValue(65536);
        
        builder.Property(e => e.JudgeMode).HasColumnName("judge_mode");
        builder.Property(e => e.SpecialJudgeCode).HasColumnName("special_judge_code");
        builder.Property(e => e.SpecialJudgeLanguage).HasColumnName("special_judge_language").HasMaxLength(20);
        builder.Property(e => e.InteractorCode).HasColumnName("interactor_code");
        builder.Property(e => e.InteractorLanguage).HasColumnName("interactor_language").HasMaxLength(20);
        builder.Property(e => e.AllowedLanguages).HasColumnName("allowed_languages").HasMaxLength(200);
        
        builder.Property(e => e.Visibility).HasColumnName("visibility");
        builder.Property(e => e.Difficulty).HasColumnName("difficulty");
        builder.Property(e => e.SubmissionCount).HasColumnName("submission_count");
        builder.Property(e => e.AcceptedCount).HasColumnName("accepted_count");
        
        builder.Property(e => e.CreatorId).HasColumnName("creator_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");
        
        builder.HasIndex(e => e.Visibility);
        builder.HasIndex(e => e.Difficulty);
        builder.HasIndex(e => e.CreatorId);
        
        builder.HasOne(e => e.Creator)
            .WithMany(e => e.CreatedProblems)
            .HasForeignKey(e => e.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("test_cases");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.ProblemId).HasColumnName("problem_id");
        builder.Property(e => e.Order).HasColumnName("order");
        builder.Property(e => e.InputPath).HasColumnName("input_path").HasMaxLength(500).IsRequired();
        builder.Property(e => e.OutputPath).HasColumnName("output_path").HasMaxLength(500).IsRequired();
        builder.Property(e => e.InputSize).HasColumnName("input_size");
        builder.Property(e => e.OutputSize).HasColumnName("output_size");
        builder.Property(e => e.Score).HasColumnName("score").HasDefaultValue(10);
        builder.Property(e => e.IsSample).HasColumnName("is_sample");
        builder.Property(e => e.Subtask).HasColumnName("subtask");
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        
        builder.HasIndex(e => new { e.ProblemId, e.Order });
        
        builder.HasOne(e => e.Problem)
            .WithMany(e => e.TestCases)
            .HasForeignKey(e => e.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.HasIndex(e => e.Name).IsUnique();
        
        builder.Property(e => e.Color).HasColumnName("color").HasMaxLength(20);
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
        builder.Property(e => e.UsageCount).HasColumnName("usage_count");
    }
}

public class ProblemTagConfiguration : IEntityTypeConfiguration<ProblemTag>
{
    public void Configure(EntityTypeBuilder<ProblemTag> builder)
    {
        builder.ToTable("problem_tags");
        
        builder.HasKey(e => new { e.ProblemId, e.TagId });
        builder.Property(e => e.ProblemId).HasColumnName("problem_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        
        builder.HasOne(e => e.Problem)
            .WithMany(e => e.ProblemTags)
            .HasForeignKey(e => e.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Tag)
            .WithMany(e => e.ProblemTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSolvedProblemConfiguration : IEntityTypeConfiguration<UserSolvedProblem>
{
    public void Configure(EntityTypeBuilder<UserSolvedProblem> builder)
    {
        builder.ToTable("user_solved_problems");
        
        builder.HasKey(e => new { e.UserId, e.ProblemId });
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.ProblemId).HasColumnName("problem_id");
        builder.Property(e => e.FirstAcceptedSubmissionId).HasColumnName("first_accepted_submission_id");
        builder.Property(e => e.SolvedAt).HasColumnName("solved_at");
        
        builder.HasOne(e => e.User)
            .WithMany(e => e.SolvedProblems)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Problem)
            .WithMany(e => e.SolvedByUsers)
            .HasForeignKey(e => e.ProblemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
