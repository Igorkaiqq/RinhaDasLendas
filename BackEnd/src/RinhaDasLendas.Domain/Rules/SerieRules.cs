using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Rules;

public static class SerieRules
{
    public const string TimeZoneId = "America/Sao_Paulo";

    private static readonly TimeZoneInfo SaoPauloTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    public static int ObterVitoriasNecessarias(SerieFormato formato) => formato switch
    {
        SerieFormato.Md3 => 2,
        SerieFormato.Md5 => 3,
        _ => throw new DomainException(MessageCodes.SeriesMustBeBestOfThreeOrFive),
    };

    public static DateOnly ObterDataCompetitiva(DateTimeOffset instante) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, SaoPauloTimeZone).DateTime);

    public static bool EstaNoPeriodoDaSeason(
        DateTimeOffset agendadaPara,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva)
    {
        var dataCompetitiva = ObterDataCompetitiva(agendadaPara);
        return dataCompetitiva >= dataInicio && dataCompetitiva < dataFimExclusiva;
    }

    public static void ValidarElegibilidadeDiaria(
        DateTimeOffset agendadaPara,
        DateOnly dataLocal,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva)
    {
        if (dataLocal != ObterDataCompetitiva(agendadaPara)
            || !EstaNoPeriodoDaSeason(agendadaPara, dataInicio, dataFimExclusiva))
        {
            throw new DomainException(MessageCodes.DailySeriesDateInvalid);
        }
    }

    public static ResultadoSerie CalcularResultado(
        SerieFormato formato,
        IReadOnlyCollection<LadoSerie> lados,
        IReadOnlyCollection<Partida> partidas)
    {
        if (lados.Count != 2)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var ladosOrdenados = lados.OrderBy(lado => lado.Ordem).ToArray();
        var confirmadas = partidas.Where(partida => partida.Estado == PartidaEstado.Confirmada).ToArray();
        var vitoriasLadoUm = confirmadas.Count(partida => partida.LadoVencedorId == ladosOrdenados[0].Id);
        var vitoriasLadoDois = confirmadas.Count(partida => partida.LadoVencedorId == ladosOrdenados[1].Id);
        var vitoriasNecessarias = ObterVitoriasNecessarias(formato);
        Guid? vencedor = vitoriasLadoUm >= vitoriasNecessarias
            ? ladosOrdenados[0].Id
            : vitoriasLadoDois >= vitoriasNecessarias
                ? ladosOrdenados[1].Id
                : null;

        return new ResultadoSerie(
            vitoriasLadoUm,
            vitoriasLadoDois,
            vitoriasNecessarias,
            confirmadas.Length,
            vencedor);
    }
}

public sealed record ResultadoSerie(
    int VitoriasLadoUm,
    int VitoriasLadoDois,
    int VitoriasNecessarias,
    int QuantidadePartidasValidas,
    Guid? LadoVencedorId);
