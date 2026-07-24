using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

internal static class AgendamentoPresencaHandlerHelpers
{
    public static DateOnly CalculateInitialMarker(
        AgendamentoPresenca agenda,
        DateTimeOffset now,
        IAgendamentoPresencaTimeZone timeZone)
    {
        var localDate = timeZone.GetLocalDate(now);
        var publicationAt = timeZone.ToUtc(localDate, agenda.HorarioPublicacaoLocal);
        return now <= publicationAt ? localDate.AddDays(-1) : localDate;
    }

    public static async Task<AgendamentoPresencaSummaryDto?> GetSummaryAsync(
        IAgendamentoPresencaRepository repository,
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await repository.GetSummaryAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var latest = await repository.GetLatestOccurrenceAsync(id, cancellationToken);
        return ToSummary(item, latest);
    }

    public static AgendamentoPresencaSummaryDto ToSummary(
        AgendamentoPresencaListItem item,
        OcorrenciaAgendamentoPresenca? latestOccurrence)
    {
        var agenda = item.Agenda;
        return new AgendamentoPresencaSummaryDto(
            agenda.Id,
            agenda.Nome,
            agenda.Observacao,
            agenda.Status,
            agenda.DiasSemana.Select(day => day.DiaSemana).Order().ToArray(),
            agenda.HorarioPublicacaoLocal,
            agenda.HorarioEncerramentoLocal,
            item.ProximaExecucaoEm,
            latestOccurrence is null ? null : ToOccurrence(latestOccurrence));
    }

    public static OcorrenciaAgendamentoPresencaSummaryDto ToOccurrence(
        OcorrenciaAgendamentoPresenca occurrence) => new(
            occurrence.Id,
            occurrence.DataLocal,
            occurrence.PublicacaoPrevistaEm,
            occurrence.EncerramentoPrevistoEm,
            occurrence.Status,
            occurrence.DraftMontagemId,
            occurrence.CodigoFalha);
}
