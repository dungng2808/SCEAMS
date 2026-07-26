using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IGenericRepository<ClubCategory> ClubCategories { get; }
    IClubRepository Clubs { get; }
    IGenericRepository<ClubMembership> ClubMemberships { get; }
    IGenericRepository<Venue> Venues { get; }
    IEventRepository Events { get; }
    IRegistrationRepository Registrations { get; }
    IGenericRepository<Attendance> Attendances { get; }
    IGenericRepository<Feedback> Feedbacks { get; }
    IGenericRepository<ChatLog> ChatLogs { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}
