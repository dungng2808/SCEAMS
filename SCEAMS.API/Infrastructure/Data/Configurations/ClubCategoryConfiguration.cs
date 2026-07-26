using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class ClubCategoryConfiguration
    : IEntityTypeConfiguration<ClubCategory>
{
    public void Configure(EntityTypeBuilder<ClubCategory> builder)
    {
        builder.ToTable("ClubCategories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.Property(category => category.Description)
            .HasMaxLength(1000);
    }
}
