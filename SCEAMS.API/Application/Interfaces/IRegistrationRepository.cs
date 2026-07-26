using SCEAMS.Domain.Entities;

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
}
