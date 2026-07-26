using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCEAMS.MVC.Models.Api;

public sealed class EventApiResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;

    [JsonConverter(typeof(EventStatusJsonConverter))]
    public string Status { get; init; } = string.Empty;

    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public EventClubSummaryApiResponse Club { get; init; } = new();
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public EventVenueSummaryApiResponse Venue { get; init; } = new();
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
    public int CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
}

public sealed class EventClubSummaryApiResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class EventVenueSummaryApiResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}

public sealed class EventODataListApiResponse
{
    [JsonPropertyName("@odata.count")]
    public int? Count { get; init; }

    [JsonPropertyName("value")]
    public List<EventApiResponse>? Value { get; init; }
}

public sealed class EventStatusJsonConverter : JsonConverter<string>
{
    public override string Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
        {
            return value switch
            {
                1 => "Draft",
                2 => "PendingApproval",
                3 => "Approved",
                4 => "Ongoing",
                5 => "Completed",
                6 => "Cancelled",
                7 => "Rejected",
                _ => value.ToString()
            };
        }

        return reader.TokenType == JsonTokenType.String
            ? reader.GetString() ?? string.Empty
            : string.Empty;
    }

    public override void Write(
        Utf8JsonWriter writer,
        string value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
