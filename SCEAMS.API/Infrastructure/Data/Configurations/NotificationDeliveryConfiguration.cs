using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class NotificationDeliveryConfiguration
    : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.NotificationType)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(item => item.CorrelationId)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(item => item.Status)
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(item => item.ErrorMessage)
            .HasMaxLength(2000);
        builder.Property(item => item.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(item => new { item.EventId, item.NotificationType })
            .IsUnique();
        builder.HasOne(item => item.Event)
            .WithMany()
            .HasForeignKey(item => item.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
