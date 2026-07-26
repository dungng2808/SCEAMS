using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class RegisterStudentRequestDto
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

    [Required]
    [StringLength(50, MinimumLength = 4)]
    [RegularExpression(
        "^[A-Za-z0-9]+$",
        ErrorMessage = "StudentCode may contain only letters and numbers.")]
    public string StudentCode { get; init; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9\\s]).+$",
        ErrorMessage = "Password must contain an uppercase letter, a lowercase letter, a number and a special character.")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(
        nameof(Password),
        ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
