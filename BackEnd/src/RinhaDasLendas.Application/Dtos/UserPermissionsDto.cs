using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record UserPermissionsDto(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions, string EffectiveRole);
