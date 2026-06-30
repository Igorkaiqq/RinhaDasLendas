namespace RinhaDasLendas.Application.Dtos;

public sealed record UsuarioAuditoriaResponseDto(
    Guid Id,
    string Acao,
    Guid? UsuarioAlvoId,
    Guid? UsuarioExecutorId,
    DateTimeOffset DataCadastro,
    string? Detalhes);
