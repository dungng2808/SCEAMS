namespace SCEAMS.Application.AI;

public sealed record EventFaqQuery(
    string OriginalQuestion,
    IReadOnlyList<string> Keywords,
    DateTime? FromUtc,
    DateTime? ToUtc,
    bool OnlyEventsWithSlots,
    int Limit = 10);
