using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Interfaces;

public interface IRegistrationRepository
    : IGenericRepository<Registration>
{
    Task<Registration?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Registration?> GetByStudentAndEventAsync(
        int studentId,
        int eventId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveForEventAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Registration> Items, int TotalItems)> GetForStudentAsync(
        int studentId,
        RegistrationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
