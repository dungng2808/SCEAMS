using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable(
            "Events",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Events_Capacity",
                    "[Capacity] > 0");
                table.HasCheckConstraint(
                    "CK_Events_TimeRange",
                    "[EndTime] > [StartTime]");
                table.HasCheckConstraint(
                    "CK_Events_RegistrationDeadline",
                    "[RegistrationDeadline] <= [StartTime]");
            });

        builder.HasKey(eventEntity => eventEntity.Id);

        builder.Property(eventEntity => eventEntity.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.Description)
            .HasMaxLength(4000);

        builder.Property(eventEntity => eventEntity.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(eventEntity => eventEntity.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(eventEntity => eventEntity.CancellationReason)
            .HasMaxLength(1000);

        builder.Property(eventEntity => eventEntity.RowVersion)
            .IsRowVersion();

        builder.HasIndex(eventEntity => new
        {
            eventEntity.VenueId,
            eventEntity.StartTime,
            eventEntity.EndTime
        });

        builder.HasIndex(eventEntity => new
        {
            eventEntity.ClubId,
            eventEntity.Status
        });

        builder.HasOne(eventEntity => eventEntity.Club)
            .WithMany(club => club.Events)
            .HasForeignKey(eventEntity => eventEntity.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eventEntity => eventEntity.Venue)
            .WithMany(venue => venue.Events)
            .HasForeignKey(eventEntity => eventEntity.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eventEntity => eventEntity.CreatedByUser)
            .WithMany(user => user.CreatedEvents)
            .HasForeignKey(eventEntity => eventEntity.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eventEntity => eventEntity.ApprovedByUser)
            .WithMany(user => user.ApprovedEvents)
            .HasForeignKey(eventEntity => eventEntity.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
