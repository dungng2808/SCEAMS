using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class UpdateVenueMaintenanceRequestDto
{
    [Required]
    public bool IsUnderMaintenance { get; init; }
}
