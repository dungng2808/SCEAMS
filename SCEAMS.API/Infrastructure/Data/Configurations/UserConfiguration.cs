using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.StudentCode)
            .HasMaxLength(50);

        builder.HasIndex(user => user.StudentCode)
            .IsUnique()
            .HasFilter("[StudentCode] IS NOT NULL");

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Property(user => user.RefreshTokenHash)
            .HasMaxLength(512);

        builder.Property(user => user.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
