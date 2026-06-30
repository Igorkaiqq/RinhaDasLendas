namespace RinhaDasLendas.Application.Dtos;

public sealed record UpdateUsuarioRolesRequestDto(IReadOnlyCollection<string> Roles);
