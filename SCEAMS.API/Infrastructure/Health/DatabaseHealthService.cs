using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;
using SCEAMS.Infrastructure.Data;
using SCEAMS.Infrastructure.Data.Seed;

namespace SCEAMS.Infrastructure.Health;

public sealed class DatabaseHealthService
    : IDatabaseHealthService
{
    private readonly SceamsDbContext _dbContext;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(
        SceamsDbContext dbContext,
        ILogger<DatabaseHealthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<DatabaseHealthResponseDto>> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database
                .CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return Result<DatabaseHealthResponseDto>.Fail(
                    "Database is unavailable.",
                    StatusCodes.Status503ServiceUnavailable);
            }

            var demoAccountCount = await _dbContext.Users.CountAsync(
                user =>
                    (user.Email == DemoSeedData.AdminEmail &&
                     user.Role == UserRole.Admin) ||
                    (user.Email == DemoSeedData.StaffEmail &&
                     user.Role == UserRole.Staff) ||
                    (user.Email == DemoSeedData.OrganizerEmail &&
                     user.Role == UserRole.Organizer) ||
                    (user.Email == DemoSeedData.StudentEmail &&
                     user.Role == UserRole.Student),
                cancellationToken);

            var hasCategory = await _dbContext.ClubCategories.AnyAsync(
                category => category.Name == DemoSeedData.CategoryName,
                cancellationToken);
            var hasClub = await _dbContext.Clubs.AnyAsync(
                club => club.Name == DemoSeedData.ClubName,
                cancellationToken);
            var hasVenue = await _dbContext.Venues.AnyAsync(
                venue => venue.Name == DemoSeedData.VenueName,
                cancellationToken);
            var hasEvent = await _dbContext.Events.AnyAsync(
                eventEntity =>
                    eventEntity.Title == DemoSeedData.EventTitle,
                cancellationToken);
            var hasRegistration = await _dbContext.Registrations.AnyAsync(
                registration =>
                    registration.Student.Email ==
                        DemoSeedData.StudentEmail &&
                    registration.Event.Title ==
                        DemoSeedData.EventTitle,
                cancellationToken);

            var demoSeedReady =
                demoAccountCount == DemoSeedData.AccountCount &&
                hasCategory &&
                hasClub &&
                hasVenue &&
                hasEvent &&
                hasRegistration;

            var response = new DatabaseHealthResponseDto(
                Database: _dbContext.Database.GetDbConnection().Database,
                Status: "Healthy",
                CanConnect: true,
                DemoSeedReady: demoSeedReady,
                DemoAccountCount: demoAccountCount);

            return Result<DatabaseHealthResponseDto>.Ok(response);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Database health check failed.");

            return Result<DatabaseHealthResponseDto>.Fail(
                "Database is unavailable.",
                StatusCodes.Status503ServiceUnavailable);
        }
    }
}
