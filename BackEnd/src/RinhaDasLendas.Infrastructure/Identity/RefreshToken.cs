namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamiliaId { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? RevogadoEm { get; set; }
    public Guid? SubstituidoPorTokenId { get; set; }
    public string? IpCriacao { get; set; }
    public string? UserAgentCriacao { get; set; }
    public string? IpRevogacao { get; set; }
    public string? MotivoRevogacao { get; set; }

    public bool EstaAtivo(DateTimeOffset agora) => RevogadoEm is null && ExpiraEm > agora;
}
