namespace SCEAMS.Application.DTOs.Chatbot;

public sealed record AiEventContextDto(
    int Id,
    string Title,
    string ClubName,
    string VenueName,
    DateTime StartTime,
    DateTime EndTime,
    int Capacity,
    int RegisteredCount,
    int SlotsRemaining);
