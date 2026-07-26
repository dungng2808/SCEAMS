using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Infrastructure.Data.Seed;

public sealed class DatabaseSeeder
{
    private readonly SceamsDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly SeedDataOptions _options;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        SceamsDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IOptions<SeedDataOptions> options,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        _options.Validate();

        await _dbContext.Database.MigrateAsync(cancellationToken);

        var executionStrategy = _dbContext.Database
            .CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            _ = await EnsureUserAsync(
                email: DemoSeedData.AdminEmail,
                fullName: "SCEAMS Administrator",
                role: UserRole.Admin,
                studentCode: null,
                password: _options.AdminPassword,
                now,
                cancellationToken);

            var staff = await EnsureUserAsync(
                email: DemoSeedData.StaffEmail,
                fullName: "Student Affairs Staff",
                role: UserRole.Staff,
                studentCode: null,
                password: _options.StaffPassword,
                now,
                cancellationToken);

            var organizer = await EnsureUserAsync(
                email: DemoSeedData.OrganizerEmail,
                fullName: "FPT AI Club Organizer",
                role: UserRole.Organizer,
                studentCode: null,
                password: _options.OrganizerPassword,
                now,
                cancellationToken);

            var student = await EnsureUserAsync(
                email: DemoSeedData.StudentEmail,
                fullName: "Demo Student",
                role: UserRole.Student,
                studentCode: "SE000001",
                password: _options.StudentPassword,
                now,
                cancellationToken);

            var category = await EnsureCategoryAsync(
                name: DemoSeedData.CategoryName,
                description: "Clubs focused on academic development, technology and innovation.",
                cancellationToken);

            var club = await EnsureClubAsync(
                name: DemoSeedData.ClubName,
                description: "A student community for artificial intelligence, machine learning and data science.",
                category,
                organizer,
                staff,
                now,
                cancellationToken);

            await EnsureMembershipAsync(
                student,
                club,
                staff,
                now,
                cancellationToken);

            var venue = await EnsureVenueAsync(
                name: DemoSeedData.VenueName,
                location: "FPT University - Building Alpha",
                capacity: 100,
                cancellationToken);

            var eventEntity = await EnsureEventAsync(
                title: DemoSeedData.EventTitle,
                description: "An introductory workshop about practical AI applications for university students.",
                club,
                venue,
                organizer,
                staff,
                now,
                cancellationToken);

            await EnsureRegistrationAsync(
                student,
                eventEntity,
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Development seed completed. Users: {UserCount}, Clubs: {ClubCount}, Events: {EventCount}, Registrations: {RegistrationCount}.",
                await _dbContext.Users.CountAsync(cancellationToken),
                await _dbContext.Clubs.CountAsync(cancellationToken),
                await _dbContext.Events.CountAsync(cancellationToken),
                await _dbContext.Registrations.CountAsync(
                    cancellationToken));

        });
    }

    private async Task<User> EnsureUserAsync(
        string email,
        string fullName,
        UserRole role,
        string? studentCode,
        string password,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var existingUser = await _dbContext.Users
            .SingleOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);

        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Role = role,
            StudentCode = studentCode,
            IsActive = true,
            CreatedAt = createdAt
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            password);

        await _dbContext.Users.AddAsync(
            user,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private async Task<ClubCategory> EnsureCategoryAsync(
        string name,
        string description,
        CancellationToken cancellationToken)
    {
        var existingCategory = await _dbContext.ClubCategories
            .SingleOrDefaultAsync(
                category => category.Name == name,
                cancellationToken);

        if (existingCategory is not null)
        {
            return existingCategory;
        }

        var category = new ClubCategory
        {
            Name = name,
            Description = description
        };

        await _dbContext.ClubCategories.AddAsync(
            category,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category;
    }

    private async Task<Club> EnsureClubAsync(
        string name,
        string description,
        ClubCategory category,
        User organizer,
        User reviewer,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var existingClub = await _dbContext.Clubs
            .SingleOrDefaultAsync(
                club => club.Name == name,
                cancellationToken);

        if (existingClub is not null)
        {
            return existingClub;
        }

        var club = new Club
        {
            Name = name,
            Description = description,
            CategoryId = category.Id,
            Status = ClubStatus.Approved,
            CreatedByUserId = organizer.Id,
            CreatedAt = createdAt,
            ReviewedByUserId = reviewer.Id,
            ReviewedAt = createdAt
        };

        await _dbContext.Clubs.AddAsync(
            club,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return club;
    }

    private async Task EnsureMembershipAsync(
        User student,
        Club club,
        User reviewer,
        DateTime joinedAt,
        CancellationToken cancellationToken)
    {
        var membershipExists = await _dbContext.ClubMemberships
            .AnyAsync(
                membership =>
                    membership.StudentId == student.Id &&
                    membership.ClubId == club.Id,
                cancellationToken);

        if (membershipExists)
        {
            return;
        }

        var membership = new ClubMembership
        {
            StudentId = student.Id,
            ClubId = club.Id,
            RoleInClub = "Member",
            JoinDate = joinedAt,
            Status = ClubMembershipStatus.Active,
            DecidedByUserId = reviewer.Id,
            DecisionAt = joinedAt
        };

        await _dbContext.ClubMemberships.AddAsync(
            membership,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Venue> EnsureVenueAsync(
        string name,
        string location,
        int capacity,
        CancellationToken cancellationToken)
    {
        var existingVenue = await _dbContext.Venues
            .SingleOrDefaultAsync(
                venue =>
                    venue.Name == name &&
                    venue.Location == location,
                cancellationToken);

        if (existingVenue is not null)
        {
            return existingVenue;
        }

        var venue = new Venue
        {
            Name = name,
            Location = location,
            Capacity = capacity,
            IsUnderMaintenance = false
        };

        await _dbContext.Venues.AddAsync(
            venue,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return venue;
    }

    private async Task<Event> EnsureEventAsync(
        string title,
        string description,
        Club club,
        Venue venue,
        User organizer,
        User approver,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var existingEvent = await _dbContext.Events
            .SingleOrDefaultAsync(
                eventEntity =>
                    eventEntity.Title == title &&
                    eventEntity.ClubId == club.Id,
                cancellationToken);

        if (existingEvent is not null)
        {
            return existingEvent;
        }

        var startTime = DateTime.UtcNow.Date
            .AddDays(14)
            .AddHours(1);

        var eventEntity = new Event
        {
            ClubId = club.Id,
            VenueId = venue.Id,
            Title = title,
            Description = description,
            StartTime = startTime,
            EndTime = startTime.AddHours(3),
            RegistrationDeadline = startTime.AddDays(-2),
            Capacity = 80,
            Status = EventStatus.Approved,
            CreatedByUserId = organizer.Id,
            CreatedAt = createdAt,
            ApprovedByUserId = approver.Id,
            ApprovedAt = createdAt
        };

        await _dbContext.Events.AddAsync(
            eventEntity,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return eventEntity;
    }

    private async Task EnsureRegistrationAsync(
        User student,
        Event eventEntity,
        DateTime registeredAt,
        CancellationToken cancellationToken)
    {
        var registrationExists = await _dbContext.Registrations
            .AnyAsync(
                registration =>
                    registration.StudentId == student.Id &&
                    registration.EventId == eventEntity.Id,
                cancellationToken);

        if (registrationExists)
        {
            return;
        }

        var registration = new Registration
        {
            StudentId = student.Id,
            EventId = eventEntity.Id,
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = registeredAt
        };

        await _dbContext.Registrations.AddAsync(
            registration,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
