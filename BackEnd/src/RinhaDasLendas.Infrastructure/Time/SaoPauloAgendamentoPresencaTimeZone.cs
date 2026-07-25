using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Infrastructure.Time;

public sealed class SaoPauloAgendamentoPresencaTimeZone : IAgendamentoPresencaTimeZone
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateOnly GetLocalDate(DateTimeOffset instant)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, SaoPaulo).DateTime);
    }

    public DateTimeOffset ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (SaoPaulo.IsInvalidTime(local) || SaoPaulo.IsAmbiguousTime(local))
        {
            throw new DomainException(MessageCodes.PresenceScheduleTimeZoneInvalid);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, SaoPaulo), TimeSpan.Zero);
    }
}
