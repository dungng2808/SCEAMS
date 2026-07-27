using Microsoft.EntityFrameworkCore;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Infrastructure.Data;

public class SceamsDbContext : DbContext
{
    public SceamsDbContext(DbContextOptions<SceamsDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ClubCategory> ClubCategories => Set<ClubCategory>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<ChatLog> ChatLogs => Set<ChatLog>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SceamsDbContext).Assembly);
    }
}
