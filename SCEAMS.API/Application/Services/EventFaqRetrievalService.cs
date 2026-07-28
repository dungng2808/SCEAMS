using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using SCEAMS.Application.AI;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs.Chatbot;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class EventFaqRetrievalService : IEventFaqRetrievalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public EventFaqRetrievalService(
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<EventFaqRetrievalResponseDto>> RetrieveAsync(
        EventFaqRetrievalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var question = request.Question.Trim();
        if (question.Length < 2)
        {
            return Result<EventFaqRetrievalResponseDto>.Fail(
                "Question phải có ít nhất 2 ký tự.",
                StatusCodes.Status400BadRequest);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var parsed = EventFaqQueryParser.Parse(question, nowUtc);
        var query = _unitOfWork.Events
            .GetQueryable()
            .Where(eventEntity =>
                eventEntity.Status == EventStatus.Approved &&
                eventEntity.StartTime >= nowUtc);

        if (parsed.FromUtc.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.StartTime >= parsed.FromUtc.Value);
        }

        if (parsed.ToUtc.HasValue)
        {
            query = query.Where(eventEntity =>
                eventEntity.StartTime < parsed.ToUtc.Value);
        }

        if (parsed.OnlyEventsWithSlots)
        {
            query = query.Where(eventEntity =>
                eventEntity.Registrations.Count(registration =>
                    registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended) < eventEntity.Capacity);
        }

        if (parsed.Keywords.Count > 0)
        {
            query = query.Where(eventEntity =>
                parsed.Keywords.Any(keyword =>
                    eventEntity.Title.ToLower().Contains(keyword) ||
                    (eventEntity.Description != null &&
                        eventEntity.Description.ToLower().Contains(keyword)) ||
                    eventEntity.Club.Name.ToLower().Contains(keyword) ||
                    eventEntity.Venue.Name.ToLower().Contains(keyword)));
        }

        var events = await query
            .OrderBy(eventEntity => eventEntity.StartTime)
            .Take(parsed.Limit)
            .Select(eventEntity => new EventFaqEventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                ClubName = eventEntity.Club.Name,
                VenueName = eventEntity.Venue.Name,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,
                Capacity = eventEntity.Capacity,
                RegisteredCount = eventEntity.Registrations.Count(registration =>
                    registration.Status == RegistrationStatus.Confirmed ||
                    registration.Status == RegistrationStatus.Attended),
                SlotsRemaining = Math.Max(
                    0,
                    eventEntity.Capacity - eventEntity.Registrations.Count(registration =>
                        registration.Status == RegistrationStatus.Confirmed ||
                        registration.Status == RegistrationStatus.Attended))
            })
            .ToListAsync(cancellationToken);

        return Result<EventFaqRetrievalResponseDto>.Ok(new EventFaqRetrievalResponseDto
        {
            Question = question,
            RelatedEvents = events
        });
    }
}
