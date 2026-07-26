using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class AttendanceConfiguration
    : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");
        builder.HasKey(attendance => attendance.Id);

        builder.HasIndex(attendance => attendance.RegistrationId)
            .IsUnique();

        builder.Property(attendance => attendance.CheckInTime)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(attendance => attendance.Registration)
            .WithOne(registration => registration.Attendance)
            .HasForeignKey<Attendance>(attendance => attendance.RegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attendance => attendance.CheckedInByUser)
            .WithMany(user => user.CheckedInAttendances)
            .HasForeignKey(attendance => attendance.CheckedInByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
