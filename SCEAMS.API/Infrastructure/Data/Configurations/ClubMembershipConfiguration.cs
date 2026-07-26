using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class ClubMembershipConfiguration
    : IEntityTypeConfiguration<ClubMembership>
{
    public void Configure(EntityTypeBuilder<ClubMembership> builder)
    {
        builder.ToTable("ClubMemberships");
        builder.HasKey(membership => membership.Id);

        builder.HasIndex(membership => new
        {
            membership.StudentId,
            membership.ClubId
        })
            .IsUnique();

        builder.Property(membership => membership.RoleInClub)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(membership => membership.JoinDate)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(membership => membership.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(membership => membership.RemovalReason)
            .HasMaxLength(1000);

        builder.HasOne(membership => membership.Student)
            .WithMany(user => user.ClubMemberships)
            .HasForeignKey(membership => membership.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(membership => membership.Club)
            .WithMany(club => club.Memberships)
            .HasForeignKey(membership => membership.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(membership => membership.DecidedByUser)
            .WithMany(user => user.MembershipDecisions)
            .HasForeignKey(membership => membership.DecidedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
