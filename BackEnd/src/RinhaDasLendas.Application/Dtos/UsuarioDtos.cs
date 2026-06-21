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

public sealed record UpdateUsuarioRequestDto(string Nome);

public sealed record UpdateUsuarioRolesRequestDto(IReadOnlyCollection<string> Roles);

public sealed record ResetUsuarioPasswordRequestDto(string NovaSenha, string ConfirmacaoSenha);

public sealed record UsuarioRolesResponseDto(Guid Id, IReadOnlyCollection<string> Roles, DateTimeOffset DataAtualizacao);

public sealed record RoleResponseDto(string Nome, int Nivel);

public sealed record RoleListResponseDto(IReadOnlyCollection<RoleResponseDto> Items);

public sealed record UsuarioAuditoriaResponseDto(
    Guid Id,
    string Acao,
    Guid? UsuarioAlvoId,
    Guid? UsuarioExecutorId,
    DateTimeOffset DataCadastro,
    string? Detalhes);
