using Microsoft.AspNetCore.Identity;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTimeOffset? UltimoLoginEm { get; set; }
    public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DataAtualizacao { get; set; } = DateTimeOffset.UtcNow;
}
