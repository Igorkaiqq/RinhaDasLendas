using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class GetAgendamentoPresencaQueryHandler(
    IAgendamentoPresencaRepository repository,
    IAgendamentoPresencaTimeZone timeZone)
    : IRequestHandler<GetAgendamentoPresencaQuery, AgendamentoPresencaSummaryDto?>
{
    public async Task<AgendamentoPresencaSummaryDto?> Handle(
        GetAgendamentoPresencaQuery query,
        CancellationToken cancellationToken)
    {
        var agenda = await repository.GetByIdAsync(query.Id, false, cancellationToken);
        return agenda is null || agenda.Status == AgendamentoPresencaStatus.Arquivado
            ? null
            : AgendamentoPresencaSummaryDto.FromEntity(agenda, timeZone);
    }
}
