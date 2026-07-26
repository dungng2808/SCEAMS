using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class RefreshTokenRequestDto
{
    [Required]
    [StringLength(512, MinimumLength = 32)]
    public string RefreshToken { get; init; } = string.Empty;
}
