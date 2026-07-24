using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ListAgendamentosPresencaQueryHandler(
    IAgendamentoPresencaRepository repository,
    IAgendamentoPresencaTimeZone timeZone)
    : IRequestHandler<ListAgendamentosPresencaQuery, PaginatedResponseDto<AgendamentoPresencaSummaryDto>>
{
    public async Task<PaginatedResponseDto<AgendamentoPresencaSummaryDto>> Handle(
        ListAgendamentosPresencaQuery query,
        CancellationToken cancellationToken)
    {
        var totalItems = await repository.CountAsync(includePaused: true, cancellationToken);
        var agendas = await repository.ListAsync(includePaused: true, query.Page, query.PageSize, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        var items = new List<AgendamentoPresencaSummaryDto>(agendas.Count);
        foreach (var agenda in agendas)
        {
            var occurrences = await repository.ListOccurrencesAsync(
                agenda.Id,
                page: 1,
                pageSize: 1,
                cancellationToken);
            items.Add(AgendamentoPresencaSummaryDto.FromEntity(agenda, timeZone, occurrences?.FirstOrDefault()));
        }

        return new PaginatedResponseDto<AgendamentoPresencaSummaryDto>(
            query.Page,
            query.PageSize,
            items,
            totalItems,
            totalPages);
    }
}
