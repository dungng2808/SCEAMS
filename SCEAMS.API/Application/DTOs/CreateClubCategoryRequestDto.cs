using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CreateClubCategoryRequestDto
{
    [Required]
    [StringLength(150)]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "Name must contain a non-whitespace character.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }
}
