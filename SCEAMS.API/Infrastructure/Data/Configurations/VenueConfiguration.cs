using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable(
            "Venues",
            table => table.HasCheckConstraint(
                "CK_Venues_Capacity",
                "[Capacity] > 0"));

        builder.HasKey(venue => venue.Id);

        builder.Property(venue => venue.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(venue => venue.Location)
            .HasMaxLength(300)
            .IsRequired();

        builder.HasIndex(venue => new { venue.Name, venue.Location })
            .IsUnique();

        builder.Property(venue => venue.IsUnderMaintenance)
            .HasDefaultValue(false);
    }
}
