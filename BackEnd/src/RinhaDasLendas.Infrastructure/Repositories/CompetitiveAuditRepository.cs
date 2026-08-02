using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class CompetitiveAuditRepository(RinhaDasLendasDbContext dbContext)
    : ICompetitiveAuditRepository
{
    public Task AddAsync(
        RegistroAuditoriaCompetitiva registro,
        CancellationToken cancellationToken) =>
        dbContext.RegistrosAuditoriaCompetitiva.AddAsync(registro, cancellationToken).AsTask();

    public Task<IReadOnlyCollection<RegistroAuditoriaCompetitiva>> ListByResourceAsync(
        RecursoCompetitivoTipo recursoTipo,
        Guid recursoId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> CountByResourceAsync(
        RecursoCompetitivoTipo recursoTipo,
        Guid recursoId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
