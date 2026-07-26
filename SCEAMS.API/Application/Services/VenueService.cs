using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Services;

public sealed class VenueService : IVenueService
{
    private readonly IUnitOfWork _unitOfWork;

    public VenueService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VenueResponseDto>> CreateVenueAsync(
        CreateVenueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var location = request.Location.Trim();
        var duplicateExists = await _unitOfWork.Venues.AnyAsync(
            venue => venue.Name.ToLower() == name.ToLower() &&
                     venue.Location.ToLower() == location.ToLower(),
            cancellationToken);

        if (duplicateExists)
        {
            return Result<VenueResponseDto>.Fail(
                $"Địa điểm '{name}' tại '{location}' đã tồn tại.",
                StatusCodes.Status409Conflict);
        }

        var venue = new Venue
        {
            Name = name,
            Location = location,
            Capacity = request.Capacity,
            IsUnderMaintenance = false
        };

        await _unitOfWork.Venues.AddAsync(venue, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<VenueResponseDto>.Created(new VenueResponseDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Capacity = venue.Capacity,
            IsUnderMaintenance = venue.IsUnderMaintenance
        });
    }

    public async Task<Result<PagedResult<VenueResponseDto>>> GetVenuesAsync(
        string? search,
        bool? maintenance,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var queryable = _unitOfWork.Venues.GetQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            queryable = queryable.Where(venue =>
                venue.Name.ToLower().Contains(normalizedSearch) ||
                venue.Location.ToLower().Contains(normalizedSearch));
        }

        if (maintenance.HasValue)
        {
            queryable = queryable.Where(venue =>
                venue.IsUnderMaintenance == maintenance.Value);
        }

        var totalItems = await queryable.CountAsync(cancellationToken);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var items = await queryable
            .OrderBy(venue => venue.Name)
            .ThenBy(venue => venue.Location)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(venue => new VenueResponseDto
            {
                Id = venue.Id,
                Name = venue.Name,
                Location = venue.Location,
                Capacity = venue.Capacity,
                IsUnderMaintenance = venue.IsUnderMaintenance
            })
            .ToListAsync(cancellationToken);

        return Result<PagedResult<VenueResponseDto>>.Ok(
            new PagedResult<VenueResponseDto>(items, totalItems));
    }

    public async Task<Result<VenueResponseDto>> UpdateVenueAsync(
        int id,
        UpdateVenueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var venue = await _unitOfWork.Venues.GetByIdAsync(id, cancellationToken);
        if (venue == null)
        {
            return Result<VenueResponseDto>.Fail(
                $"Địa điểm với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var name = request.Name.Trim();
        var location = request.Location.Trim();
        var duplicateExists = await _unitOfWork.Venues.AnyAsync(
            candidate => candidate.Id != id &&
                         candidate.Name.ToLower() == name.ToLower() &&
                         candidate.Location.ToLower() == location.ToLower(),
            cancellationToken);

        if (duplicateExists)
        {
            return Result<VenueResponseDto>.Fail(
                $"Địa điểm '{name}' tại '{location}' đã tồn tại.",
                StatusCodes.Status409Conflict);
        }

        if (request.Capacity < venue.Capacity)
        {
            var upcomingRegistrations = await _unitOfWork.Events
                .GetUpcomingConfirmedRegistrationCountForVenueAsync(
                    id,
                    DateTime.UtcNow,
                    cancellationToken);

            if (request.Capacity < upcomingRegistrations)
            {
                return Result<VenueResponseDto>.Fail(
                    $"Không thể giảm sức chứa xuống {request.Capacity}. Có {upcomingRegistrations} đăng ký hợp lệ ở các Event sắp tới.",
                    StatusCodes.Status409Conflict);
            }
        }

        venue.Name = name;
        venue.Location = location;
        venue.Capacity = request.Capacity;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<VenueResponseDto>.Ok(new VenueResponseDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Capacity = venue.Capacity,
            IsUnderMaintenance = venue.IsUnderMaintenance
        });
    }

    public async Task<Result<VenueResponseDto>> UpdateMaintenanceAsync(
        int id,
        UpdateVenueMaintenanceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var venue = await _unitOfWork.Venues.GetByIdAsync(id, cancellationToken);
        if (venue == null)
        {
            return Result<VenueResponseDto>.Fail(
                $"Địa điểm với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (request.IsUnderMaintenance && !venue.IsUnderMaintenance)
        {
            var conflicts = await _unitOfWork.Events.GetActiveEventsForVenueAsync(
                id,
                DateTime.UtcNow,
                cancellationToken);

            if (conflicts.Count > 0)
            {
                var conflictDtos = conflicts.Select(eventEntity =>
                    new VenueMaintenanceConflictDto
                    {
                        EventId = eventEntity.Id,
                        Title = eventEntity.Title,
                        Status = eventEntity.Status.ToString(),
                        StartTime = eventEntity.StartTime,
                        EndTime = eventEntity.EndTime
                    }).ToList();

                return Result<VenueResponseDto>.Fail(
                    "Không thể bật bảo trì vì địa điểm đang được sử dụng bởi Event Approved/Ongoing.",
                    StatusCodes.Status409Conflict,
                    conflictDtos);
            }
        }

        venue.IsUnderMaintenance = request.IsUnderMaintenance;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<VenueResponseDto>.Ok(new VenueResponseDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Location = venue.Location,
            Capacity = venue.Capacity,
            IsUnderMaintenance = venue.IsUnderMaintenance
        });
    }

    public async Task<Result> DeleteVenueAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var venue = await _unitOfWork.Venues.GetByIdAsync(id, cancellationToken);
        if (venue == null)
        {
            return Result.Fail(
                $"Địa điểm với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isReferenced = await _unitOfWork.Events.AnyAsync(
            eventEntity => eventEntity.VenueId == id,
            cancellationToken);

        if (isReferenced)
        {
            return Result.Fail(
                "Không thể xóa địa điểm đã từng được Event tham chiếu. Hãy bật trạng thái bảo trì thay vì xóa.",
                StatusCodes.Status409Conflict);
        }

        _unitOfWork.Venues.Delete(venue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.NoContent("Địa điểm đã được xóa.");
    }
}
