namespace RinhaDasLendas.Application.Dtos;

public sealed record UsuarioResponseDto(
    Guid Id,
    string Nome,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool Ativo,
    Guid? JogadorId,
    DiscordLinkStatusDto Discord,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao,
    DateTimeOffset? UltimoLoginEm,
    IReadOnlyCollection<string> AcoesPermitidas);
