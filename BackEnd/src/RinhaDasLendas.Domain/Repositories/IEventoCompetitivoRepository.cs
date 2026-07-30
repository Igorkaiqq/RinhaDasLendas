using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Domain.Repositories;

public interface IEventoCompetitivoRepository
{
    Task<EventoCompetitivo?> GetWithTeamsAsync(Guid eventoId, CancellationToken cancellationToken);
    Task AddAsync(EventoCompetitivo evento, CancellationToken cancellationToken);
}
