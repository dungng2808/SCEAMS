using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;
using SCEAMS.Infrastructure.Repositories;

namespace SCEAMS.Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SceamsDbContext _context;

    private IUserRepository? _users;
    private IClubCategoryRepository? _clubCategories;
    private IClubRepository? _clubs;
    private IGenericRepository<ClubMembership>? _clubMemberships;
    private IGenericRepository<Venue>? _venues;
    private IEventRepository? _events;
    private IRegistrationRepository? _registrations;
    private IGenericRepository<Attendance>? _attendances;
    private IGenericRepository<Feedback>? _feedbacks;
    private IGenericRepository<ChatLog>? _chatLogs;

    public UnitOfWork(SceamsDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IClubCategoryRepository ClubCategories =>
        _clubCategories ??=
            new ClubCategoryRepository(_context);

    public IClubRepository Clubs =>
        _clubs ??= new ClubRepository(_context);

    public IGenericRepository<ClubMembership> ClubMemberships =>
        _clubMemberships ??=
            new GenericRepository<ClubMembership>(_context);

    public IGenericRepository<Venue> Venues =>
        _venues ??= new GenericRepository<Venue>(_context);

    public IEventRepository Events =>
        _events ??= new EventRepository(_context);

    public IRegistrationRepository Registrations =>
        _registrations ??= new RegistrationRepository(_context);

    public IGenericRepository<Attendance> Attendances =>
        _attendances ??=
            new GenericRepository<Attendance>(_context);

    public IGenericRepository<Feedback> Feedbacks =>
        _feedbacks ??= new GenericRepository<Feedback>(_context);

    public IGenericRepository<ChatLog> ChatLogs =>
        _chatLogs ??= new GenericRepository<ChatLog>(_context);

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        return new UnitOfWorkTransaction(transaction);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
