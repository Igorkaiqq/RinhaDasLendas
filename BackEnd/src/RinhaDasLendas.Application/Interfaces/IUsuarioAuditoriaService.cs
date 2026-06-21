namespace RinhaDasLendas.Application.Interfaces;

public interface IUsuarioAuditoriaService
{
    Task RegistrarAsync(string acao, Guid? usuarioAlvoId, Guid? usuarioExecutorId, string? detalhes, string? ip, string? userAgent, CancellationToken cancellationToken);
}
