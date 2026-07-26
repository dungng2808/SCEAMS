using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class ClubCategoryApiClient
    : IClubCategoryApiClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public ClubCategoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateClubCategoryApiResult>
        CreateClubCategoryAsync(
            CreateClubCategoryApiRequest request,
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/club-categories",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var category = await response.Content
                .ReadFromJsonAsync<ClubCategoryApiResponse>(
                    cancellationToken: cancellationToken);

            return new CreateClubCategoryApiResult
            {
                IsSuccess = category is not null,
                Category = category,
                ErrorMessage = category is null
                    ? "API trả về danh mục vừa tạo không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem =
                DeserializeOrDefault<ValidationProblemApiResponse>(
                    content);

            if (validationProblem?.Errors is { Count: > 0 })
            {
                return new CreateClubCategoryApiResult
                {
                    FieldErrors = validationProblem.Errors
                };
            }

            return new CreateClubCategoryApiResult
            {
                ErrorMessage =
                    "Thông tin danh mục không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = DeserializeOrDefault<ApiErrorResponse>(
                content);

            return new CreateClubCategoryApiResult
            {
                FieldErrors = new Dictionary<string, string[]>
                {
                    ["Name"] =
                    [
                        conflict?.Message ==
                            "Club category name already exists."
                            ? "Tên danh mục này đã tồn tại."
                            : "Tên danh mục đã được sử dụng."
                    ]
                }
            };
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized =>
                new CreateClubCategoryApiResult
                {
                    IsUnauthorized = true,
                    ErrorMessage =
                        "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                },
            HttpStatusCode.Forbidden =>
                new CreateClubCategoryApiResult
                {
                    IsForbidden = true,
                    ErrorMessage =
                        "Bạn không có quyền tạo danh mục câu lạc bộ."
                },
            _ => new CreateClubCategoryApiResult
            {
                ErrorMessage =
                    "Không thể tạo danh mục vào lúc này."
            }
        };
    }

    public async Task<ClubCategoryListApiResult>
        GetClubCategoriesAsync(
            CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/club-categories",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var categories = await response.Content
                .ReadFromJsonAsync<List<ClubCategoryApiResponse>>(
                    cancellationToken: cancellationToken);

            return new ClubCategoryListApiResult
            {
                IsSuccess = categories is not null,
                Categories = categories ?? [],
                ErrorMessage = categories is null
                    ? "API trả về danh sách danh mục không hợp lệ."
                    : null
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new ClubCategoryListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage =
                    "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        return new ClubCategoryListApiResult
        {
            ErrorMessage =
                "Không thể tải danh mục câu lạc bộ vào lúc này."
        };
    }

    private static T? DeserializeOrDefault<T>(string content)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(
                content,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ApiErrorResponse(string? Message);

    private sealed record ValidationProblemApiResponse(
        Dictionary<string, string[]> Errors);
}
