using System.Text.Json.Serialization;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class ClubResponseDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClubStatus Status { get; init; }

    public int CreatedByUserId { get; init; }

    public string CreatedByUserName { get; init; } = string.Empty;
    public int ActiveMemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? DissolvedAt { get; init; }
}
