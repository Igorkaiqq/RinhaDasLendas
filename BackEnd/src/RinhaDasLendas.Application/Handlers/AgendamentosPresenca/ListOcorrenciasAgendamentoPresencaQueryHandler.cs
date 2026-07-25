using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ListOcorrenciasAgendamentoPresencaQueryHandler(IAgendamentoPresencaRepository repository)
    : IRequestHandler<ListOcorrenciasAgendamentoPresencaQuery, PaginatedResponseDto<OcorrenciaAgendamentoPresencaSummaryDto>?>
{
    public async Task<PaginatedResponseDto<OcorrenciaAgendamentoPresencaSummaryDto>?> Handle(
        ListOcorrenciasAgendamentoPresencaQuery query,
        CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAsync(query.AgendamentoId, cancellationToken))
        {
            return null;
        }

        var totalItems = await repository.CountOccurrencesAsync(query.AgendamentoId, cancellationToken);
        var occurrences = await repository.ListOccurrencesAsync(
            query.AgendamentoId,
            query.Page,
            query.PageSize,
            cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        return new PaginatedResponseDto<OcorrenciaAgendamentoPresencaSummaryDto>(
            query.Page,
            query.PageSize,
            occurrences.Select(AgendamentoPresencaHandlerHelpers.ToOccurrence).ToArray(),
            totalItems,
            totalPages);
    }
}
