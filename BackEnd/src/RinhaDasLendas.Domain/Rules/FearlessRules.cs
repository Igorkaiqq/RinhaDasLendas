using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Rules;

public static class FearlessRules
{
    public static void ValidarPicks(
        IReadOnlyCollection<LadoSerie> lados,
        IReadOnlyCollection<(Guid LadoSerieId, int ChampionId, int Ordem)> picks)
    {
        if (lados.Count != 2 || picks is null || picks.Count != 10)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var ladoIds = lados.Select(lado => lado.Id).ToHashSet();
        if (picks.Any(pick => !ladoIds.Contains(pick.LadoSerieId)
                || pick.ChampionId <= 0
                || pick.Ordem is < 1 or > 5)
            || picks.Select(pick => pick.ChampionId).Distinct().Count() != 10)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        foreach (var ladoId in ladoIds)
        {
            var picksDoLado = picks.Where(pick => pick.LadoSerieId == ladoId).ToArray();
            if (picksDoLado.Length != 5
                || !picksDoLado.Select(pick => pick.Ordem).Order().SequenceEqual([1, 2, 3, 4, 5]))
            {
                throw new DomainException(MessageCodes.ValidationError);
            }
        }
    }

    public static IReadOnlySet<int> ReconstruirBloqueios(
        IReadOnlyCollection<Partida> partidas,
        int ordemPartida)
    {
        if (ordemPartida <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return partidas
            .Where(partida => partida.Ordem < ordemPartida && ContribuiParaBloqueio(partida))
            .SelectMany(partida => partida.Picks)
            .Where(pick => pick.Valido)
            .Select(pick => pick.ChampionId)
            .ToHashSet();
    }

    public static bool PossuiConflito(Partida partida, IReadOnlySet<int> bloqueiosAnteriores) =>
        ContribuiParaBloqueio(partida)
        && partida.Picks.Any(pick => pick.Valido && bloqueiosAnteriores.Contains(pick.ChampionId));

    private static bool ContribuiParaBloqueio(Partida partida) =>
        partida.Estado == Enums.PartidaEstado.Confirmada
        || partida.Estado == Enums.PartidaEstado.Remake
            && partida.DecisaoPicksRemake == Enums.DecisaoPicksRemake.PreservarPicks;
}
