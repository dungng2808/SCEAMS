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
}
