namespace SCEAMS.Application.DTOs;

public sealed record HealthResponseDto(
    string Service,
    string Version,
    string Status);
