using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed record ClubResponseDto(
    int Id,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    ClubStatus Status,
    int CreatedByUserId,
    string CreatedByUserName,
    int ActiveMemberCount,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? RejectionReason,
    DateTime? DissolvedAt);
