namespace RinhaDasLendas.Application.Interfaces;

public interface IAgendamentoPresencaTimeZone
{
    DateOnly GetLocalDate(DateTimeOffset instant);
    DateTimeOffset ToUtc(DateOnly date, TimeOnly time);
}
