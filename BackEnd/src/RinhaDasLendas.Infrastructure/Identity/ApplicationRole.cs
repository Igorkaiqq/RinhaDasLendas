using Microsoft.AspNetCore.Identity;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public int NivelHierarquico { get; set; }
}
