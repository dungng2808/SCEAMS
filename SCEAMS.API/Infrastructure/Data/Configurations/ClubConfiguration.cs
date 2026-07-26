using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.ToTable("Clubs");
        builder.HasKey(club => club.Id);

        builder.Property(club => club.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(club => club.Name)
            .IsUnique();

        builder.Property(club => club.Description)
            .HasMaxLength(2000);

        builder.Property(club => club.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(club => club.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(club => club.RejectionReason)
            .HasMaxLength(1000);

        builder.HasOne(club => club.Category)
            .WithMany(category => category.Clubs)
            .HasForeignKey(club => club.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(club => club.CreatedByUser)
            .WithMany(user => user.CreatedClubs)
            .HasForeignKey(club => club.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(club => club.ReviewedByUser)
            .WithMany(user => user.ReviewedClubs)
            .HasForeignKey(club => club.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
