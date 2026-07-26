using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class CreateUserRequestDto
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

    [Required]
    [EnumDataType(typeof(UserRole))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; init; }

    public bool IsActive { get; init; } = true;

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
