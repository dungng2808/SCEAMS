using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class VenueApiClient : IVenueApiClient
{
    private readonly HttpClient _httpClient;

    public VenueApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VenueApiResult> GetVenueAsync(
        int venueId,
        CancellationToken cancellationToken = default)
    {
        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            var result = await GetVenuesAsync(
                search: null,
                maintenance: null,
                page,
                pageSize,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return new VenueApiResult
                {
                    IsUnauthorized = result.IsUnauthorized,
                    IsForbidden = result.IsForbidden,
                    ErrorMessage = result.ErrorMessage
                };
            }

            var venue = result.Venues.FirstOrDefault(item => item.Id == venueId);
            if (venue is not null)
            {
                return new VenueApiResult
                {
                    IsSuccess = true,
                    Venue = venue
                };
            }

            if (!result.HasNextPage)
            {
                return new VenueApiResult
                {
                    IsNotFound = true,
                    ErrorMessage = "Địa điểm không tồn tại."
                };
            }

            page++;
        }
    }

    public async Task<CreateVenueApiResult> CreateVenueAsync(
        CreateVenueApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/venues",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var venue = await response.Content
                .ReadFromJsonAsync<VenueApiResponse>(
                    cancellationToken: cancellationToken);

            return new CreateVenueApiResult
            {
                IsSuccess = true,
                Venue = venue
            };
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new CreateVenueApiResult
            {
                IsValidationError = true,
                ErrorMessage = "Thông tin địa điểm chưa hợp lệ. Vui lòng kiểm tra lại."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new CreateVenueApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new CreateVenueApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Chỉ Admin hoặc Staff mới có thể tạo địa điểm."
            };
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new CreateVenueApiResult
            {
                IsConflict = true,
                ErrorMessage = "Tên và vị trí địa điểm này đã tồn tại."
            };
        }

        return new CreateVenueApiResult
        {
            ErrorMessage = "Không thể tạo địa điểm vào lúc này."
        };
    }

    public async Task<UpdateVenueApiResult> UpdateVenueAsync(
        int venueId,
        UpdateVenueApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/venues/{venueId}",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var venue = await response.Content
                .ReadFromJsonAsync<VenueApiResponse>(
                    cancellationToken: cancellationToken);

            return new UpdateVenueApiResult
            {
                IsSuccess = venue is not null,
                Venue = venue,
                ErrorMessage = venue is null
                    ? "API trả về địa điểm vừa cập nhật không hợp lệ."
                    : null
            };
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new UpdateVenueApiResult
            {
                IsValidationError = true,
                ErrorMessage = "Thông tin địa điểm chưa hợp lệ. Vui lòng kiểm tra lại."
            },
            HttpStatusCode.Unauthorized => new UpdateVenueApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new UpdateVenueApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Chỉ Admin hoặc Staff mới có thể sửa địa điểm."
            },
            HttpStatusCode.NotFound => new UpdateVenueApiResult
            {
                IsNotFound = true,
                ErrorMessage = "Địa điểm không tồn tại."
            },
            HttpStatusCode.Conflict => new UpdateVenueApiResult
            {
                IsConflict = true,
                ErrorMessage = ExtractApiMessage(errorContent) ??
                    "Không thể cập nhật vì dữ liệu địa điểm đang xung đột."
            },
            _ => new UpdateVenueApiResult
            {
                ErrorMessage = "Không thể cập nhật địa điểm vào lúc này."
            }
        };
    }

    public async Task<VenueListApiResult> GetVenuesAsync(
        string? search,
        bool? maintenance,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var query = new List<string>
        {
            $"page={normalizedPage}",
            $"pageSize={normalizedPageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Insert(0, $"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (maintenance.HasValue)
        {
            query.Add($"maintenance={maintenance.Value.ToString().ToLowerInvariant()}");
        }

        using var response = await _httpClient.GetAsync(
            $"api/venues?{string.Join("&", query)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var pagedResponse = await response.Content
                .ReadFromJsonAsync<PagedApiResponse<VenueApiResponse>>(
                    cancellationToken: cancellationToken);

            return new VenueListApiResult
            {
                IsSuccess = true,
                Venues = pagedResponse?.Items ?? [],
                TotalItems = pagedResponse?.TotalItems ?? 0,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new VenueListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new VenueListApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem danh sách địa điểm."
            };
        }

        return new VenueListApiResult
        {
            ErrorMessage = "Không thể tải danh sách địa điểm vào lúc này."
        };
    }

    private sealed record PagedApiResponse<T>(List<T>? Items, int TotalItems);

    private static string? ExtractApiMessage(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
