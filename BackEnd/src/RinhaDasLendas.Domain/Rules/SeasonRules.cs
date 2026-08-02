using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Rules;

public static class SeasonRules
{
    public static void ValidarDadosIntrinsecos(
        int ano,
        int ordemNoAno,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva)
    {
        if (ano is < 2009 or > 9999 || ordemNoAno <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        ValidarPeriodo(dataInicio, dataFimExclusiva);
    }

    public static void ValidarPeriodo(DateOnly dataInicio, DateOnly dataFimExclusiva)
    {
        if (dataFimExclusiva <= dataInicio)
        {
            throw new DomainException(MessageCodes.SeasonPeriodInvalid);
        }
    }

    public static void ValidarInclusao(Season candidata, IReadOnlyCollection<Season> existentes)
    {
        if (candidata is null || existentes is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        ValidarDadosIntrinsecos(
            candidata.Ano,
            candidata.OrdemNoAno,
            candidata.DataInicio,
            candidata.DataFimExclusiva);

        foreach (var existente in existentes.Where(item => item.Id != candidata.Id))
        {
            if (PeriodosSobrepostos(candidata, existente))
            {
                throw new DomainException(MessageCodes.SeasonPeriodOverlap);
            }

            if (existente.Ano != candidata.Ano)
            {
                continue;
            }

            if (existente.OrdemNoAno == candidata.OrdemNoAno
                || (existente.OrdemNoAno < candidata.OrdemNoAno && existente.DataInicio >= candidata.DataInicio)
                || (existente.OrdemNoAno > candidata.OrdemNoAno && existente.DataInicio <= candidata.DataInicio))
            {
                throw new DomainException(MessageCodes.ValidationError);
            }
        }
    }

    private static bool PeriodosSobrepostos(Season primeira, Season segunda) =>
        primeira.DataInicio < segunda.DataFimExclusiva
        && segunda.DataInicio < primeira.DataFimExclusiva;
}
