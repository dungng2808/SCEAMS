using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class ChatLogConfiguration
    : IEntityTypeConfiguration<ChatLog>
{
    public void Configure(EntityTypeBuilder<ChatLog> builder)
    {
        builder.ToTable("ChatLogs");
        builder.HasKey(chatLog => chatLog.Id);

        builder.Property(chatLog => chatLog.Question)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(chatLog => chatLog.RelatedEventIds)
            .IsRequired();

        builder.Property(chatLog => chatLog.AnswerText)
            .IsRequired();

        builder.Property(chatLog => chatLog.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(chatLog => chatLog.Student)
            .WithMany(user => user.ChatLogs)
            .HasForeignKey(chatLog => chatLog.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
