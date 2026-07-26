namespace SCEAMS.Application.DTOs;

public sealed record DatabaseHealthResponseDto(
    string Database,
    string Status,
    bool CanConnect,
    bool DemoSeedReady,
    int DemoAccountCount);
