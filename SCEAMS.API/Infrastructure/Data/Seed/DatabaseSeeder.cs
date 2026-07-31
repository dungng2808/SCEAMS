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

            await EnsureDemoStateDataAsync(
                category,
                club,
                venue,
                organizer,
                staff,
                student,
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

    private async Task EnsureDemoStateDataAsync(
        ClubCategory category,
        Club approvedClub,
        Venue venue,
        User organizer,
        User reviewer,
        User student,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var pendingClub = await EnsureClubStateAsync(
            DemoSeedData.PendingClubName,
            ClubStatus.PendingApproval,
            category,
            organizer,
            reviewer,
            createdAt,
            cancellationToken);
        var rejectedClub = await EnsureClubStateAsync(
            DemoSeedData.RejectedClubName,
            ClubStatus.Rejected,
            category,
            organizer,
            reviewer,
            createdAt,
            cancellationToken);
        var dissolvedClub = await EnsureClubStateAsync(
            DemoSeedData.DissolvedClubName,
            ClubStatus.Dissolved,
            category,
            organizer,
            reviewer,
            createdAt,
            cancellationToken);

        await EnsureMembershipStateAsync(
            student,
            pendingClub,
            ClubMembershipStatus.Pending,
            reviewer,
            createdAt,
            cancellationToken);
        await EnsureMembershipStateAsync(
            student,
            rejectedClub,
            ClubMembershipStatus.Rejected,
            reviewer,
            createdAt,
            cancellationToken);
        await EnsureMembershipStateAsync(
            student,
            dissolvedClub,
            ClubMembershipStatus.Removed,
            reviewer,
            createdAt,
            cancellationToken);

        var now = DateTime.UtcNow;
        var approvedEvent = await _dbContext.Events.SingleAsync(
            eventEntity =>
                eventEntity.Title == DemoSeedData.EventTitle &&
                eventEntity.ClubId == approvedClub.Id,
            cancellationToken);
        var draftEvent = await EnsureEventStateAsync(
            DemoSeedData.DraftEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.Draft,
            now.AddDays(30),
            capacity: 40,
            createdAt,
            cancellationToken);
        var pendingEvent = await EnsureEventStateAsync(
            DemoSeedData.PendingEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.PendingApproval,
            now.AddDays(20),
            capacity: 40,
            createdAt,
            cancellationToken);
        _ = await EnsureEventStateAsync(
            DemoSeedData.CompletedEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.Completed,
            now.AddDays(-14),
            capacity: 80,
            createdAt,
            cancellationToken);
        var cancelledEvent = await EnsureEventStateAsync(
            DemoSeedData.CancelledEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.Cancelled,
            now.AddDays(10),
            capacity: 80,
            createdAt,
            cancellationToken);
        _ = await EnsureEventStateAsync(
            DemoSeedData.RejectedEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.Rejected,
            now.AddDays(25),
            capacity: 40,
            createdAt,
            cancellationToken);
        var fullCapacityEvent = await EnsureEventStateAsync(
            DemoSeedData.FullCapacityEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.Approved,
            now.AddDays(12),
            capacity: 1,
            createdAt,
            cancellationToken);
        var deadlinePassedEvent = await EnsureEventStateAsync(
            DemoSeedData.DeadlinePassedEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.Approved,
            now.AddDays(8),
            capacity: 20,
            createdAt,
            cancellationToken,
            registrationDeadline: now.AddDays(-1));
        var conflictEvent = await EnsureEventStateAsync(
            DemoSeedData.VenueConflictEventTitle,
            approvedClub,
            venue,
            organizer,
            EventStatus.PendingApproval,
            approvedEvent.StartTime.AddMinutes(30),
            capacity: 20,
            createdAt,
            cancellationToken);
        if (conflictEvent.StartTime != approvedEvent.StartTime.AddMinutes(30))
        {
            conflictEvent.StartTime = approvedEvent.StartTime.AddMinutes(30);
            conflictEvent.EndTime = approvedEvent.EndTime.AddMinutes(30);
            conflictEvent.RegistrationDeadline = conflictEvent.StartTime.AddDays(-2);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await EnsureRegistrationStateAsync(
            student,
            draftEvent,
            RegistrationStatus.Pending,
            now,
            cancellationToken);
        await EnsureRegistrationStateAsync(
            student,
            cancelledEvent,
            RegistrationStatus.CancelledByStudent,
            now,
            cancellationToken);
        await EnsureRegistrationStateAsync(
            student,
            fullCapacityEvent,
            RegistrationStatus.Confirmed,
            now,
            cancellationToken);
        await EnsureRegistrationStateAsync(
            student,
            deadlinePassedEvent,
            RegistrationStatus.Confirmed,
            now,
            cancellationToken);

        var completedEvent = await _dbContext.Events
            .SingleAsync(
                eventEntity =>
                    eventEntity.Title == DemoSeedData.CompletedEventTitle &&
                    eventEntity.ClubId == approvedClub.Id,
                cancellationToken);
        var attendedRegistration = await EnsureRegistrationStateAsync(
            student,
            completedEvent,
            RegistrationStatus.Attended,
            now.AddDays(-15),
            cancellationToken);
        await EnsureAttendanceAsync(
            attendedRegistration,
            reviewer,
            now.AddDays(-14),
            cancellationToken);
        await EnsureFeedbackAsync(
            student,
            completedEvent,
            cancellationToken);

        // Keep named variables in the seed output so reviewers can identify
        // the intended venue-conflict and capacity/deadline fixtures quickly.
        _ = pendingEvent;
        _ = conflictEvent;
    }

    private async Task<Club> EnsureClubStateAsync(
        string name,
        ClubStatus status,
        ClubCategory category,
        User organizer,
        User reviewer,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var club = await _dbContext.Clubs.SingleOrDefaultAsync(
            item => item.Name == name,
            cancellationToken);
        if (club is not null)
        {
            return club;
        }

        club = new Club
        {
            Name = name,
            Description = "Dữ liệu demo phục vụ kiểm thử workflow.",
            CategoryId = category.Id,
            Status = status,
            CreatedByUserId = organizer.Id,
            CreatedAt = createdAt,
            ReviewedByUserId = status == ClubStatus.PendingApproval
                ? null
                : reviewer.Id,
            ReviewedAt = status == ClubStatus.PendingApproval
                ? null
                : createdAt,
            RejectionReason = status == ClubStatus.Rejected
                ? "Dữ liệu demo trạng thái Rejected."
                : null,
            DissolvedAt = status == ClubStatus.Dissolved
                ? createdAt
                : null
        };
        await _dbContext.Clubs.AddAsync(club, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return club;
    }

    private async Task EnsureMembershipStateAsync(
        User student,
        Club club,
        ClubMembershipStatus status,
        User reviewer,
        DateTime joinedAt,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ClubMemberships.AnyAsync(
            item => item.StudentId == student.Id && item.ClubId == club.Id,
            cancellationToken);
        if (exists)
        {
            return;
        }

        await _dbContext.ClubMemberships.AddAsync(
            new ClubMembership
            {
                StudentId = student.Id,
                ClubId = club.Id,
                RoleInClub = "Member",
                JoinDate = joinedAt,
                Status = status,
                DecidedByUserId = status == ClubMembershipStatus.Pending
                    ? null
                    : reviewer.Id,
                DecisionAt = status == ClubMembershipStatus.Pending
                    ? null
                    : joinedAt,
                RemovalReason = status == ClubMembershipStatus.Removed
                    ? "Dữ liệu demo trạng thái Removed."
                    : null
            },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Event> EnsureEventStateAsync(
        string title,
        Club club,
        Venue venue,
        User organizer,
        EventStatus status,
        DateTime startTime,
        int capacity,
        DateTime createdAt,
        CancellationToken cancellationToken,
        DateTime? registrationDeadline = null)
    {
        var eventEntity = await _dbContext.Events.SingleOrDefaultAsync(
            item => item.Title == title && item.ClubId == club.Id,
            cancellationToken);
        if (eventEntity is not null)
        {
            return eventEntity;
        }

        eventEntity = new Event
        {
            ClubId = club.Id,
            VenueId = venue.Id,
            Title = title,
            Description = "Dữ liệu demo phục vụ kiểm thử workflow.",
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            RegistrationDeadline = registrationDeadline ?? startTime.AddDays(-2),
            Capacity = capacity,
            Status = status,
            CreatedByUserId = organizer.Id,
            CreatedAt = createdAt,
            ApprovedByUserId = status is EventStatus.Approved or EventStatus.Ongoing or EventStatus.Completed
                ? organizer.Id
                : null,
            ApprovedAt = status is EventStatus.Approved or EventStatus.Ongoing or EventStatus.Completed
                ? createdAt
                : null,
            RejectionReason = status == EventStatus.Rejected
                ? "Dữ liệu demo trạng thái Rejected."
                : null,
            CancellationReason = status == EventStatus.Cancelled
                ? "Dữ liệu demo trạng thái Cancelled."
                : null
        };
        await _dbContext.Events.AddAsync(eventEntity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return eventEntity;
    }

    private async Task<Registration> EnsureRegistrationStateAsync(
        User student,
        Event eventEntity,
        RegistrationStatus status,
        DateTime registeredAt,
        CancellationToken cancellationToken)
    {
        var registration = await _dbContext.Registrations.SingleOrDefaultAsync(
            item => item.StudentId == student.Id && item.EventId == eventEntity.Id,
            cancellationToken);
        if (registration is not null)
        {
            return registration;
        }

        registration = new Registration
        {
            StudentId = student.Id,
            EventId = eventEntity.Id,
            Status = status,
            RegisteredAt = registeredAt,
            CancelledAt = status == RegistrationStatus.CancelledByStudent
                ? registeredAt
                : null
        };
        await _dbContext.Registrations.AddAsync(registration, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return registration;
    }

    private async Task EnsureAttendanceAsync(
        Registration registration,
        User reviewer,
        DateTime checkInTime,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Attendances.AnyAsync(
            item => item.RegistrationId == registration.Id,
            cancellationToken);
        if (exists)
        {
            return;
        }

        await _dbContext.Attendances.AddAsync(
            new Attendance
            {
                RegistrationId = registration.Id,
                CheckInTime = checkInTime,
                CheckedInByUserId = reviewer.Id
            },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureFeedbackAsync(
        User student,
        Event eventEntity,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Feedbacks.AnyAsync(
            item => item.EventId == eventEntity.Id && item.StudentId == student.Id,
            cancellationToken);
        if (exists)
        {
            return;
        }

        await _dbContext.Feedbacks.AddAsync(
            new Feedback
            {
                EventId = eventEntity.Id,
                StudentId = student.Id,
                Rating = 5,
                Comment = "Dữ liệu demo feedback hợp lệ.",
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
