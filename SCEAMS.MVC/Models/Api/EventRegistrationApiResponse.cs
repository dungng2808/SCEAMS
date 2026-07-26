using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCEAMS.MVC.Models.Api;

public sealed class EventRegistrationApiResponse
{
    public int Id { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public DateTime EventStartTime { get; init; }
    public DateTime EventEndTime { get; init; }
    [JsonConverter(typeof(RegistrationStatusJsonConverter))]
    public string Status { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
    public int? CheckedInByUserId { get; init; }
    public string? CheckedInByUserName { get; init; }
}

public sealed class RegistrationStatusJsonConverter : JsonConverter<string>
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
                1 => "Pending",
                2 => "Confirmed",
                3 => "Attended",
                4 => "CancelledByStudent",
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
