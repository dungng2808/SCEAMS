using System.ComponentModel.DataAnnotations;

namespace SCEAMS.Application.DTOs;

public sealed class CreateRegistrationRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "EventId không hợp lệ.")]
    public int EventId { get; init; }
}
