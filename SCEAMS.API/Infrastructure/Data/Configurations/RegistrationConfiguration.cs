using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class RegistrationConfiguration
    : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("Registrations");
        builder.HasKey(registration => registration.Id);

        builder.HasIndex(registration => new
        {
            registration.StudentId,
            registration.EventId
        })
            .IsUnique();

        builder.Property(registration => registration.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(registration => registration.RegisteredAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(registration => registration.Student)
            .WithMany(user => user.Registrations)
            .HasForeignKey(registration => registration.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(registration => registration.Event)
            .WithMany(eventEntity => eventEntity.Registrations)
            .HasForeignKey(registration => registration.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
