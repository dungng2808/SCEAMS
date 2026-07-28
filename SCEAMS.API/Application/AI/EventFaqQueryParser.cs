using System.Globalization;
using System.Text;

namespace SCEAMS.Application.AI;

public static class EventFaqQueryParser
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "cho", "toi", "minh", "em", "anh", "chi", "cac", "cua", "va", "o",
        "the", "nao", "nhung", "su", "kien", "event", "clb", "club", "ve",
        "mot", "nhat", "xin", "tim", "tim", "co", "con", "trong", "den",
        "tu", "ngay", "gio", "hoi", "thao", "hom", "nay", "tuan", "thang"
    };

    public static EventFaqQuery Parse(
        string question,
        DateTime nowUtc,
        TimeZoneInfo? businessTimeZone = null)
    {
        var original = question.Trim();
        var normalized = RemoveDiacritics(original).ToLowerInvariant();
        var timeZone = businessTimeZone ?? ResolveBusinessTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            timeZone);

        DateTime? fromLocal = null;
        DateTime? toLocal = null;
        if (normalized.Contains("hom nay", StringComparison.Ordinal))
        {
            fromLocal = localNow.Date;
            toLocal = fromLocal.Value.AddDays(1);
        }
        else if (normalized.Contains("tuan nay", StringComparison.Ordinal))
        {
            var monday = localNow.Date.AddDays(-(int)localNow.DayOfWeek +
                (localNow.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
            fromLocal = monday;
            toLocal = monday.AddDays(7);
        }
        else if (normalized.Contains("thang nay", StringComparison.Ordinal))
        {
            fromLocal = new DateTime(localNow.Year, localNow.Month, 1);
            toLocal = fromLocal.Value.AddMonths(1);
        }

        var originalTokens = Tokenize(original.ToLowerInvariant());
        var normalizedTokens = Tokenize(normalized);
        var keywords = originalTokens
            .Zip(normalizedTokens, (originalToken, normalizedToken) =>
                new { originalToken, normalizedToken })
            .Where(pair => (pair.normalizedToken.Length >= 3 ||
                    pair.normalizedToken == "ai") &&
                !StopWords.Contains(pair.normalizedToken))
            .SelectMany(pair => new[] { pair.originalToken, pair.normalizedToken })
            .Distinct(StringComparer.Ordinal)
            .Take(8)
            .ToArray();

        return new EventFaqQuery(
            original,
            keywords,
            fromLocal.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(fromLocal.Value, timeZone)
                : null,
            toLocal.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(toLocal.Value, timeZone)
                : null,
            normalized.Contains("con cho", StringComparison.Ordinal) ||
            normalized.Contains("con slot", StringComparison.Ordinal) ||
            normalized.Contains("available", StringComparison.Ordinal),
            10);
    }

    private static IReadOnlyList<string> Tokenize(string value)
    {
        return value
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '!', '?', '/', '\\', '-', '_'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character == 'đ' ? 'd' : character == 'Đ' ? 'D' : character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
