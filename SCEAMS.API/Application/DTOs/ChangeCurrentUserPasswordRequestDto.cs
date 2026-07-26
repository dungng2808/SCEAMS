using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class ChangeCurrentUserPasswordRequestDto
{
    [Required]
    [StringLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9\\s]).+$",
        ErrorMessage = "NewPassword must contain an uppercase letter, a lowercase letter, a number and a special character.")]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "NewPassword and confirmation password do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
