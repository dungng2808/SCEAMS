using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class RegistrationResponseDto
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public RegistrationStatus Status { get; init; }
    public DateTime RegisteredAt { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
}
