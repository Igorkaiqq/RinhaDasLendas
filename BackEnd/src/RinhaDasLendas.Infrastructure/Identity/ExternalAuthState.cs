namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class ExternalAuthState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StateHash { get; set; } = string.Empty;
    public string Flow { get; set; } = string.Empty;
    public Guid? UsuarioId { get; set; }
    public string? ReturnUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => ConsumedAt is null && ExpiresAt > now;
}
