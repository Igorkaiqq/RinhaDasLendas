using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class PickPartida
{
    private Partida? _partida;

    private PickPartida()
    {
    }

    internal PickPartida(
        Partida partida,
        Guid ladoSerieId,
        int championId,
        int ordem,
        DateTimeOffset registradoEm)
        : this(partida?.Id ?? Guid.Empty, ladoSerieId, championId, ordem, registradoEm)
    {
        _partida = partida;
    }

    internal PickPartida(
        Guid partidaId,
        Guid ladoSerieId,
        int championId,
        int ordem,
        DateTimeOffset registradoEm)
    {
        if (partidaId == Guid.Empty
            || ladoSerieId == Guid.Empty
            || championId <= 0
            || ordem is < 1 or > 5)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Id = Guid.NewGuid();
        PartidaId = partidaId;
        LadoSerieId = ladoSerieId;
        ChampionId = championId;
        Ordem = ordem;
        VersaoFato = 1;
        Valido = true;
        RegistradoEm = registradoEm.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid LadoSerieId { get; private set; }
    public int ChampionId { get; private set; }
    public int Ordem { get; private set; }
    public int VersaoFato { get; private set; }
    public bool Valido { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }

    internal static PickPartida CriarNovaVersao(
        Partida partida,
        Guid ladoSerieId,
        int championId,
        int ordem,
        int versaoFato,
        DateTimeOffset registradoEm)
    {
        var pick = new PickPartida(partida, ladoSerieId, championId, ordem, registradoEm)
        {
            VersaoFato = versaoFato,
        };
        return pick;
    }

    internal void Invalidar() => Valido = false;
}
