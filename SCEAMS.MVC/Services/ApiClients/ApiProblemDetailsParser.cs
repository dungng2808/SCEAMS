using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public static class ApiProblemDetailsParser
{
    public static ApiProblemDetails? Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ApiProblemDetails>(
                content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? GetMessage(string content)
    {
        var problem = Parse(content);
        return problem?.Detail ?? problem?.Message ?? problem?.Title;
    }
}
