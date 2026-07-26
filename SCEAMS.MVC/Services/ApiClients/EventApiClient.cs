using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class EventApiClient : IEventApiClient
{
    private readonly HttpClient _httpClient;

    public EventApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateEventApiResult> CreateEventAsync(
        CreateEventApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/events",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new CreateEventApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về Event vừa tạo không hợp lệ."
                    : null
            };
        }

        var message = await response.Content.ReadAsStringAsync(cancellationToken);
        var error = ExtractMessage(message);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new CreateEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new CreateEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = error ?? "Bạn không có quyền tạo Event Draft."
            },
            HttpStatusCode.NotFound => new CreateEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = error ?? "Club hoặc Venue không tồn tại."
            },
            HttpStatusCode.Conflict => new CreateEventApiResult
            {
                IsConflict = true,
                ErrorMessage = error ?? "Dữ liệu Club/Venue/Event đang xung đột."
            },
            _ => new CreateEventApiResult
            {
                ErrorMessage = error ?? "Thông tin Event chưa hợp lệ."
            }
        };
    }

    public async Task<UpdateEventApiResult> UpdateEventAsync(
        int eventId,
        UpdateEventApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/events/{eventId}",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new UpdateEventApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về Event vừa cập nhật không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var error = ExtractMessage(content);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UpdateEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new UpdateEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = error ?? "Bạn không có quyền sửa Event này."
            },
            HttpStatusCode.NotFound => new UpdateEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = error ?? "Event không tồn tại."
            },
            HttpStatusCode.Conflict => new UpdateEventApiResult
            {
                IsConflict = true,
                ErrorMessage = error ?? "Event đang ở trạng thái không thể sửa."
            },
            _ => new UpdateEventApiResult
            {
                ErrorMessage = error ?? "Thông tin Event chưa hợp lệ."
            }
        };
    }

    public async Task<SubmitEventApiResult> SubmitEventAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsync(
            $"api/events/{eventId}/submit",
            content: null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new SubmitEventApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về Event vừa submit không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var error = ExtractMessage(content);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new SubmitEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new SubmitEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = error ?? "Bạn không có quyền gửi Event này."
            },
            HttpStatusCode.NotFound => new SubmitEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = error ?? "Event không tồn tại."
            },
            HttpStatusCode.Conflict => new SubmitEventApiResult
            {
                IsConflict = true,
                ErrorMessage = error ?? "Event không còn ở trạng thái Draft."
            },
            _ => new SubmitEventApiResult
            {
                ErrorMessage = error ?? "Không thể gửi Event để duyệt."
            }
        };
    }

    public async Task<EventListApiResult> GetPendingApprovalEventsAsync(
        int? clubId,
        int? venueId,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var query = new List<string>();
        if (clubId is > 0) query.Add($"clubId={clubId.Value}");
        if (venueId is > 0) query.Add($"venueId={venueId.Value}");
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(FormatDate(from.Value))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(FormatDate(to.Value))}");
        query.Add($"page={normalizedPage}");
        query.Add($"pageSize={normalizedPageSize}");

        using var response = await _httpClient.GetAsync(
            $"api/events/pending-approval?{string.Join("&", query)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<PagedApiResponse<EventApiResponse>>(
                    cancellationToken: cancellationToken);
            return new EventListApiResult
            {
                IsSuccess = true,
                Events = payload?.Items ?? [],
                TotalItems = payload?.TotalItems ?? 0,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new EventListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new EventListApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Chỉ Admin hoặc Staff mới có thể xem queue duyệt Event."
            };
        }

        return new EventListApiResult
        {
            ErrorMessage = "Không thể tải queue Event chờ duyệt."
        };
    }

    public async Task<ApproveEventApiResult> ApproveEventAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsync(
            $"api/events/{eventId}/approve",
            content: null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new ApproveEventApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var error = DeserializeOrDefault<ApprovalErrorResponse>(content);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ApproveEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new ApproveEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = error?.Message ?? "Bạn không có quyền duyệt Event."
            },
            HttpStatusCode.NotFound => new ApproveEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = error?.Message ?? "Event không tồn tại."
            },
            HttpStatusCode.Conflict => new ApproveEventApiResult
            {
                IsConflict = true,
                Conflicts = error?.Conflicts ?? [],
                ErrorMessage = error?.Message ?? "Event đang xung đột lịch."
            },
            _ => new ApproveEventApiResult
            {
                ErrorMessage = error?.Message ?? "Không thể duyệt Event."
            }
        };
    }

    public async Task<RejectEventApiResult> RejectEventAsync(
        int eventId,
        RejectEventApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/events/{eventId}/reject",
            request,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new RejectEventApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về Event đã từ chối không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractMessage(content);
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new RejectEventApiResult
            {
                ErrorMessage = message ?? "Lý do từ chối không hợp lệ."
            },
            HttpStatusCode.Unauthorized => new RejectEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new RejectEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = message ?? "Bạn không có quyền từ chối Event."
            },
            HttpStatusCode.NotFound => new RejectEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = message ?? "Event không tồn tại."
            },
            HttpStatusCode.Conflict => new RejectEventApiResult
            {
                IsConflict = true,
                ErrorMessage = message ?? "Event không còn ở trạng thái chờ duyệt."
            },
            _ => new RejectEventApiResult
            {
                ErrorMessage = message ?? "Không thể từ chối Event."
            }
        };
    }

    public async Task<CancelEventApiResult> CancelEventAsync(
        int eventId,
        CancelEventApiRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/events/{eventId}/cancel",
            request,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new CancelEventApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về Event đã hủy không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractMessage(content);
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new CancelEventApiResult
            {
                ErrorMessage = message ?? "Lý do hủy không hợp lệ."
            },
            HttpStatusCode.Unauthorized => new CancelEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new CancelEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = message ?? "Bạn không có quyền hủy Event."
            },
            HttpStatusCode.NotFound => new CancelEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = message ?? "Event không tồn tại."
            },
            HttpStatusCode.Conflict => new CancelEventApiResult
            {
                IsConflict = true,
                ErrorMessage = message ?? "Event không thể hủy ở trạng thái hiện tại."
            },
            _ => new CancelEventApiResult
            {
                ErrorMessage = message ?? "Không thể hủy Event."
            }
        };
    }

    public async Task<RegisterEventApiResult> RegisterEventAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/registrations",
            new RegisterEventApiRequest { EventId = eventId },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var registration = await response.Content
                .ReadFromJsonAsync<RegisterEventApiResponse>(
                    cancellationToken: cancellationToken);
            return new RegisterEventApiResult
            {
                IsSuccess = registration is not null,
                Registration = registration,
                ErrorMessage = registration is null
                    ? "API trả về registration không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractMessage(content);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new RegisterEventApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new RegisterEventApiResult
            {
                IsForbidden = true,
                ErrorMessage = message ?? "Chỉ Student mới có thể đăng ký Event."
            },
            HttpStatusCode.NotFound => new RegisterEventApiResult
            {
                IsNotFound = true,
                ErrorMessage = message ?? "Event không tồn tại."
            },
            HttpStatusCode.Conflict => new RegisterEventApiResult
            {
                IsConflict = true,
                ErrorMessage = message ?? "Không thể đăng ký Event."
            },
            _ => new RegisterEventApiResult
            {
                ErrorMessage = message ?? "Không thể đăng ký Event."
            }
        };
    }

    public async Task<CancelRegistrationApiResult> CancelRegistrationAsync(
        int registrationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsync(
            $"api/registrations/{registrationId}/cancel",
            content: null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var registration = await response.Content
                .ReadFromJsonAsync<RegisterEventApiResponse>(
                    cancellationToken: cancellationToken);
            return new CancelRegistrationApiResult
            {
                IsSuccess = registration is not null,
                Registration = registration,
                ErrorMessage = registration is null
                    ? "API trả về registration đã hủy không hợp lệ."
                    : null
            };
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractMessage(content);
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new CancelRegistrationApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            },
            HttpStatusCode.Forbidden => new CancelRegistrationApiResult
            {
                IsForbidden = true,
                ErrorMessage = message ?? "Bạn không có quyền hủy registration."
            },
            HttpStatusCode.NotFound => new CancelRegistrationApiResult
            {
                IsNotFound = true,
                ErrorMessage = message ?? "Registration không tồn tại."
            },
            HttpStatusCode.Conflict => new CancelRegistrationApiResult
            {
                IsConflict = true,
                ErrorMessage = message ?? "Registration không thể hủy ở thời điểm hiện tại."
            },
            _ => new CancelRegistrationApiResult
            {
                ErrorMessage = message ?? "Không thể hủy registration."
            }
        };
    }

    public async Task<EventDetailApiResult> GetEventByIdAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/events/{eventId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var eventItem = await response.Content
                .ReadFromJsonAsync<EventDetailApiResponse>(
                    cancellationToken: cancellationToken);
            return new EventDetailApiResult
            {
                IsSuccess = eventItem is not null,
                Event = eventItem,
                ErrorMessage = eventItem is null
                    ? "API trả về chi tiết Event không hợp lệ."
                    : null
            };
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new EventDetailApiResult
            {
                IsNotFound = true,
                ErrorMessage = "Event không tồn tại hoặc bạn không có quyền xem."
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new EventDetailApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new EventDetailApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem Event này."
            };
        }

        return new EventDetailApiResult
        {
            ErrorMessage = "Không thể tải chi tiết Event vào lúc này."
        };
    }

    public async Task<EventListApiResult> GetEventsAsync(
        string? search,
        int? clubId,
        DateTime? from,
        DateTime? to,
        string? status,
        bool? hasSlots,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escapedSearch = search.Trim()
                .Replace("'", "''")
                .ToLowerInvariant();
            filters.Add(
                $"(contains(tolower(Title), '{escapedSearch}') or contains(tolower(ClubName), '{escapedSearch}') or contains(tolower(VenueName), '{escapedSearch}'))");
        }

        if (clubId is > 0)
        {
            filters.Add($"ClubId eq {clubId.Value}");
        }

        if (from.HasValue)
        {
            filters.Add($"StartTime ge {FormatDate(from.Value)}");
        }

        if (to.HasValue)
        {
            filters.Add($"StartTime lt {FormatDate(to.Value.Date.AddDays(1))}");
        }

        var allowedStatuses = new HashSet<string>(
            ["Draft", "PendingApproval", "Approved", "Ongoing", "Completed", "Cancelled", "Rejected"],
            StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(status) && allowedStatuses.Contains(status))
        {
            filters.Add($"Status eq '{status}'");
        }

        if (hasSlots == true)
        {
            filters.Add("SlotsRemaining gt 0");
        }
        else if (hasSlots == false)
        {
            filters.Add("SlotsRemaining eq 0");
        }

        var query = new List<string>();
        if (filters.Count > 0)
        {
            query.Add($"$filter={Uri.EscapeDataString(string.Join(" and ", filters))}");
        }

        query.Add($"$orderby={Uri.EscapeDataString("StartTime asc")}");
        query.Add($"$skip={(normalizedPage - 1) * normalizedPageSize}");
        query.Add($"$top={normalizedPageSize}");
        query.Add("$count=true");

        using var response = await _httpClient.GetAsync(
            $"api/events?{string.Join("&", query)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content
                .ReadFromJsonAsync<EventODataListApiResponse>(
                    cancellationToken: cancellationToken);
            var events = payload?.Value ?? [];
            return new EventListApiResult
            {
                IsSuccess = true,
                Events = events,
                TotalItems = payload?.Count ?? events.Count,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            };
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new EventListApiResult
            {
                IsUnauthorized = true,
                ErrorMessage = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
            };
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new EventListApiResult
            {
                IsForbidden = true,
                ErrorMessage = "Bạn không có quyền xem danh sách sự kiện."
            };
        }

        return new EventListApiResult
        {
            ErrorMessage = "Không thể tải danh sách sự kiện vào lúc này."
        };
    }

    private static string FormatDate(DateTime dateTime)
    {
        return dateTime.ToUniversalTime()
            .ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? ExtractMessage(string content)
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

    private sealed record PagedApiResponse<T>(List<T>? Items, int TotalItems);

    private sealed record ApprovalErrorResponse(
        string? Message,
        List<EventApprovalConflictApiResponse>? Conflicts);

    private static T? DeserializeOrDefault<T>(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
