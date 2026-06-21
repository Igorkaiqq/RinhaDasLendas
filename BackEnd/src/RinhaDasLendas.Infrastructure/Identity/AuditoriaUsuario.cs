namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class AuditoriaUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UsuarioAlvoId { get; set; }
    public Guid? UsuarioExecutorId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string? Detalhes { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.UtcNow;
}
