using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Partida
{
    private readonly List<PickPartida> _picks = [];

    private Partida()
    {
    }

    internal Partida(Guid serieId, int ordem, DateTimeOffset criadaEm)
    {
        if (serieId == Guid.Empty || ordem <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var criadaEmUtc = criadaEm.ToUniversalTime();

        Id = Guid.NewGuid();
        SerieId = serieId;
        Ordem = ordem;
        Estado = PartidaEstado.Rascunho;
        CriadaEm = criadaEmUtc;
        AtualizadaEm = criadaEmUtc;
    }

    public Guid Id { get; private set; }
    public Guid SerieId { get; private set; }
    public int Ordem { get; private set; }
    public PartidaEstado Estado { get; private set; }
    public Guid? LadoVencedorId { get; private set; }
    public MotivoTerminoPartida? MotivoTermino { get; private set; }
    public DecisaoPicksRemake? DecisaoPicksRemake { get; private set; }
    public bool ConflitoFearless { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }
    public DateTimeOffset? ConfirmadaEm { get; private set; }
    public IReadOnlyCollection<PickPartida> Picks => _picks.AsReadOnly();
}
