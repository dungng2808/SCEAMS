using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class UpdateUserActiveStatusRequestDto
{
    [Required]
    public bool? IsActive { get; init; }
}
