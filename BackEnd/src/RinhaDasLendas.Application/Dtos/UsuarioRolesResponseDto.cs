namespace RinhaDasLendas.Application.Dtos;

public sealed record UsuarioRolesResponseDto(Guid Id, IReadOnlyCollection<string> Roles, DateTimeOffset DataAtualizacao);
