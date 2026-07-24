using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;

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
}
