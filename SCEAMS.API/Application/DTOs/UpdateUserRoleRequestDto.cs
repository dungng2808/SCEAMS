using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class UpdateUserRoleRequestDto
{
    [Required]
    [EnumDataType(typeof(UserRole))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole? Role { get; init; }
}
