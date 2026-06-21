namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class VinculoDiscord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string DiscordUserId { get; set; } = string.Empty;
    public string? DiscordUsername { get; set; }
    public string? DiscordGlobalName { get; set; }
    public string? DiscordAvatarHash { get; set; }
    public DateTimeOffset VinculadoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DesvinculadoEm { get; set; }
    public string? Escopos { get; set; }
}
