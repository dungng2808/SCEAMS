using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Services;

public sealed class ClubCategoryService : IClubCategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClubCategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClubCategoryResponseDto>>
        CreateClubCategoryAsync(
            CreateClubCategoryRequestDto request,
            CancellationToken cancellationToken = default)
    {
        var name = NormalizeRequiredValue(request.Name);

        if (name.Length == 0)
        {
            return Result<ClubCategoryResponseDto>.Fail(
                "Club category name is required.",
                StatusCodes.Status400BadRequest);
        }

        var normalizedName = name.ToUpperInvariant();

        if (await _unitOfWork.ClubCategories.NameExistsAsync(
                normalizedName,
                cancellationToken))
        {
            return Result<ClubCategoryResponseDto>.Fail(
                "Club category name already exists.",
                StatusCodes.Status409Conflict);
        }

        var category = new ClubCategory
        {
            Name = name,
            Description = NormalizeOptionalValue(
                request.Description)
        };

        await _unitOfWork.ClubCategories.AddAsync(
            category,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ClubCategoryResponseDto>.Created(
            new ClubCategoryResponseDto(
                category.Id,
                category.Name,
                category.Description),
            "Club category created successfully.");
    }

    public async Task<
        Result<IReadOnlyList<ClubCategoryResponseDto>>>
        GetClubCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.ClubCategories
            .GetOrderedByNameAsync(cancellationToken);

        return Result<IReadOnlyList<ClubCategoryResponseDto>>
            .Ok(categories);
    }

    private static string NormalizeRequiredValue(string value)
    {
        return string.Join(
            ' ',
            value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
