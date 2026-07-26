using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCEAMS.MVC.Models.Api;

public sealed class ClubApiResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringOrIntConverter))]
    public string Status { get; init; } = string.Empty;

    public int CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public int ActiveMemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? DissolvedAt { get; init; }
}

public sealed class JsonStringOrIntConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var intValue))
            {
                return intValue switch
                {
                    1 => "PendingApproval",
                    2 => "Approved",
                    3 => "Rejected",
                    4 => "Dissolved",
                    _ => intValue.ToString()
                };
            }
            return reader.GetDecimal().ToString();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        return doc.RootElement.ToString();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
