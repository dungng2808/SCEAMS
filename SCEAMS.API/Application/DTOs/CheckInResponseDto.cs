using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class CheckInResponseDto
{
    public int RegistrationId { get; init; }
    public int EventId { get; init; }
    public RegistrationStatus Status { get; init; }
    public DateTime CheckInTime { get; init; }
    public int CheckedInByUserId { get; init; }
}
