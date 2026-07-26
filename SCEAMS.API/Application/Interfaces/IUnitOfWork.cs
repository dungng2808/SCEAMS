using System.Data;
using SCEAMS.Domain.Entities;

namespace SCEAMS.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IClubCategoryRepository ClubCategories { get; }
    IClubRepository Clubs { get; }
    IGenericRepository<ClubMembership> ClubMemberships { get; }
    IVenueRepository Venues { get; }
    IEventRepository Events { get; }
    IRegistrationRepository Registrations { get; }
    IGenericRepository<Attendance> Attendances { get; }
    IGenericRepository<Feedback> Feedbacks { get; }
    IGenericRepository<ChatLog> ChatLogs { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
