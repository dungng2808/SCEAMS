using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class UpdateUserProfileRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    [RegularExpression(
        ".*\\S.*",
        ErrorMessage = "FullName must contain a non-whitespace character.")]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(50, MinimumLength = 4)]
    [RegularExpression(
        "^[A-Za-z0-9]+$",
        ErrorMessage = "StudentCode may contain only letters and numbers.")]
    public string? StudentCode { get; init; }

    [StringLength(30)]
    [RegularExpression(
        "^\\+?[0-9][0-9 .()\\-]{6,28}[0-9]$",
        ErrorMessage = "PhoneNumber is not a valid phone number.")]
    public string? PhoneNumber { get; init; }
}
