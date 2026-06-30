namespace RinhaDasLendas.Application.Dtos;

public sealed record UsuarioResumoDto(
    Guid Id,
    string Nome,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool Ativo,
    Guid? JogadorId,
    bool DiscordVinculado,
    DateTimeOffset DataCadastro,
    DateTimeOffset? UltimoLoginEm,
    IReadOnlyCollection<string> AcoesPermitidas);
