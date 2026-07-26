using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CreateEventRequestDto
{
    [Required]
    [StringLength(250, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int ClubId { get; init; }

    [Range(1, int.MaxValue)]
    public int VenueId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime RegistrationDeadline { get; init; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }
}
