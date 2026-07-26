using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class UpdateCurrentUserProfileRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "FullName must contain a non-whitespace character.")]
    public string FullName { get; init; } = string.Empty;

    [StringLength(30)]
    [RegularExpression(
        "^\\+?[0-9][0-9 .()\\-]{6,28}[0-9]$",
        ErrorMessage = "PhoneNumber is not a valid phone number.")]
    public string? PhoneNumber { get; init; }
}
