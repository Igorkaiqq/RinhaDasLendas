using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface ICompetitiveAuditRepository
{
    Task<IReadOnlyCollection<RegistroAuditoriaCompetitiva>> ListByResourceAsync(RecursoCompetitivoTipo recursoTipo, Guid recursoId, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountByResourceAsync(RecursoCompetitivoTipo recursoTipo, Guid recursoId, CancellationToken cancellationToken);
    Task AddAsync(RegistroAuditoriaCompetitiva registro, CancellationToken cancellationToken);
}
