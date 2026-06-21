using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class UsuarioAuditoriaService(RinhaDasLendasDbContext dbContext) : IUsuarioAuditoriaService
{
    public async Task RegistrarAsync(string acao, Guid? usuarioAlvoId, Guid? usuarioExecutorId, string? detalhes, string? ip, string? userAgent, CancellationToken cancellationToken)
    {
        await dbContext.AuditoriaUsuarios.AddAsync(new AuditoriaUsuario
        {
            Acao = acao,
            UsuarioAlvoId = usuarioAlvoId,
            UsuarioExecutorId = usuarioExecutorId,
            Detalhes = detalhes,
            Ip = ip,
            UserAgent = userAgent,
        }, cancellationToken);
    }
}
