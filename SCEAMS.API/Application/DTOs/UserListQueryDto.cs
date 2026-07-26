using System.ComponentModel.DataAnnotations;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class UserListQueryDto
{
    [StringLength(150)]
    public string? Search { get; init; }

    [EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; init; }

    public bool? IsActive { get; init; }

    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
