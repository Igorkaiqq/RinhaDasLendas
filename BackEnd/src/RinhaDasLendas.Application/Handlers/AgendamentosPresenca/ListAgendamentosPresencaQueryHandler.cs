using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ListAgendamentosPresencaQueryHandler(
    IAgendamentoPresencaRepository repository)
    : IRequestHandler<ListAgendamentosPresencaQuery, PaginatedAgendamentoPresencaResponseDto>
{
    public async Task<PaginatedAgendamentoPresencaResponseDto> Handle(
        ListAgendamentosPresencaQuery query,
        CancellationToken cancellationToken)
    {
        var totalItems = await repository.CountAsync(includePaused: true, cancellationToken);
        var activeItems = await repository.CountAsync(includePaused: false, cancellationToken);
        var agendas = await repository.ListAsync(includePaused: true, query.Page, query.PageSize, cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        var agendaIds = agendas.Select(item => item.Agenda.Id).ToArray();
        var occurrences = await repository.ListLatestOccurrencesAsync(agendaIds, cancellationToken);
        var items = agendas.Select(item => AgendamentoPresencaHandlerHelpers.ToSummary(
            item,
            occurrences.GetValueOrDefault(item.Agenda.Id))).ToArray();

        return new PaginatedAgendamentoPresencaResponseDto(
            query.Page,
            query.PageSize,
            items,
            totalItems,
            totalPages,
            activeItems);
    }
}
