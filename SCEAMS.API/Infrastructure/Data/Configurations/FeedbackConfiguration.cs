using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class FeedbackConfiguration
    : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable(
            "Feedbacks",
            table => table.HasCheckConstraint(
                "CK_Feedbacks_Rating",
                "[Rating] BETWEEN 1 AND 5"));

        builder.HasKey(feedback => feedback.Id);

        builder.HasIndex(feedback => new
        {
            feedback.EventId,
            feedback.StudentId
        })
            .IsUnique();

        builder.Property(feedback => feedback.Comment)
            .HasMaxLength(2000);

        builder.Property(feedback => feedback.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(feedback => feedback.Event)
            .WithMany(eventEntity => eventEntity.Feedbacks)
            .HasForeignKey(feedback => feedback.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(feedback => feedback.Student)
            .WithMany(user => user.Feedbacks)
            .HasForeignKey(feedback => feedback.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
