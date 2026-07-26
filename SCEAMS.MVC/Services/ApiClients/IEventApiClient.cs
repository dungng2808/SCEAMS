using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IEventApiClient
{
    Task<CreateEventApiResult> CreateEventAsync(
        CreateEventApiRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateEventApiResult> UpdateEventAsync(
        int eventId,
        UpdateEventApiRequest request,
        CancellationToken cancellationToken = default);

    Task<SubmitEventApiResult> SubmitEventAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<EventListApiResult> GetPendingApprovalEventsAsync(
        int? clubId,
        int? venueId,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<ApproveEventApiResult> ApproveEventAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<RejectEventApiResult> RejectEventAsync(
        int eventId,
        RejectEventApiRequest request,
        CancellationToken cancellationToken = default);

    Task<CancelEventApiResult> CancelEventAsync(
        int eventId,
        CancelEventApiRequest request,
        CancellationToken cancellationToken = default);

    Task<RegisterEventApiResult> RegisterEventAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<CancelRegistrationApiResult> CancelRegistrationAsync(
        int registrationId,
        CancellationToken cancellationToken = default);

    Task<SubmitFeedbackApiResult> SubmitFeedbackAsync(
        int eventId,
        SubmitFeedbackApiRequest request,
        CancellationToken cancellationToken = default);

    Task<FeedbackSummaryApiResult> GetFeedbackSummaryAsync(
        int eventId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<EventDetailApiResult> GetEventByIdAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<EventListApiResult> GetEventsAsync(
        string? search,
        int? clubId,
        DateTime? from,
        DateTime? to,
        string? status,
        bool? hasSlots,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
