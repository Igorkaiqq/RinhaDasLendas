using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record RegisterRequestDto(string Nome, string Email, string Senha, string ConfirmacaoSenha);

public sealed record LoginRequestDto(string Email, string Senha);

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Nome,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool Ativo,
    Guid? JogadorId,
    DiscordLinkStatusDto? Discord = null);

public sealed record AuthResponseDto(string AccessToken, int ExpiresIn, AuthenticatedUserDto Usuario)
{
    [JsonIgnore]
    public string? RefreshToken { get; init; }
}

public sealed record ForgotPasswordRequestDto(string Email);

public sealed record ResetPasswordRequestDto(string Email, string Token, string NovaSenha, string ConfirmacaoSenha);

public sealed record ChangePasswordRequestDto(string SenhaAtual, string NovaSenha, string ConfirmacaoSenha);

public sealed record UpdateOwnProfileRequestDto(string Nome);

public sealed record UserPermissionsDto(IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions, string EffectiveRole);

public sealed record DiscordLinkStatusDto(bool Vinculado, string? Username, DateTimeOffset? VinculadoEm = null);

public sealed record ForgotPasswordResponseDto(string Message);

public sealed record ExternalAuthStartDto(string AuthorizationUrl);

public sealed record DiscordCallbackResultDto(string RedirectUrl, AuthResponseDto? AuthResponse = null);
