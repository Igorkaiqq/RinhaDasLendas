using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class ParticipanteEsperadoSerie
{
    private ParticipanteEsperadoSerie()
    {
    }

    internal ParticipanteEsperadoSerie(Guid ladoSerieId, Guid jogadorId, string nomeSnapshot, int ordem)
    {
        Validar(ladoSerieId, jogadorId, nomeSnapshot, ordem);

        Id = Guid.NewGuid();
        LadoSerieId = ladoSerieId;
        JogadorId = jogadorId;
        NomeSnapshot = nomeSnapshot.Trim();
        Ordem = ordem;
    }

    private ParticipanteEsperadoSerie(ParticipanteEsperadoSerie origem, Guid ladoSerieId)
    {
        Validar(ladoSerieId, origem.JogadorId, origem.NomeSnapshot, origem.Ordem);

        Id = Guid.NewGuid();
        LadoSerieId = ladoSerieId;
        JogadorId = origem.JogadorId;
        NomeSnapshot = origem.NomeSnapshot;
        Ordem = origem.Ordem;
    }

    public Guid Id { get; private set; }
    public Guid LadoSerieId { get; private set; }
    public Guid JogadorId { get; private set; }
    public string NomeSnapshot { get; private set; } = string.Empty;
    public int Ordem { get; private set; }

    internal ParticipanteEsperadoSerie CloneFor(Guid ladoSerieId) => new(this, ladoSerieId);

    internal void ValidarEstrutura() => Validar(LadoSerieId, JogadorId, NomeSnapshot, Ordem);

    private static void Validar(Guid ladoSerieId, Guid jogadorId, string nomeSnapshot, int ordem)
    {
        if (ladoSerieId == Guid.Empty || jogadorId == Guid.Empty || ordem <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nomeSnapshot))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        if (nomeSnapshot.Trim().Length > 100)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }
    }
}
