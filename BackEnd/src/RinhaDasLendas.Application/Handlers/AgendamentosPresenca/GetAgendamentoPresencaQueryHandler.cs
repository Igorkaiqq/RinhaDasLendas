using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class GetAgendamentoPresencaQueryHandler(IAgendamentoPresencaRepository repository)
    : IRequestHandler<GetAgendamentoPresencaQuery, AgendamentoPresencaSummaryDto?>
{
    public async Task<AgendamentoPresencaSummaryDto?> Handle(
        GetAgendamentoPresencaQuery query,
        CancellationToken cancellationToken)
    {
        return await AgendamentoPresencaHandlerHelpers.GetSummaryAsync(repository, query.Id, cancellationToken);
    }
}
