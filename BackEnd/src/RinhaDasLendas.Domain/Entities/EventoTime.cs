using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed record EventoTimeSnapshot(
    Guid TimeId,
    int Ordem,
    string NomeSnapshot,
    string? TagSnapshot);

public sealed class EventoTime
{
    private EventoTime()
    {
    }

    internal EventoTime(Guid eventoId, EventoTimeSnapshot snapshot)
    {
        if (eventoId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(snapshot.NomeSnapshot))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        var nomeNormalizado = snapshot.NomeSnapshot.Trim();
        var tagNormalizada = snapshot.TagSnapshot?.Trim();
        if (nomeNormalizado.Length > 100 || tagNormalizada?.Length > 10)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        Id = Guid.NewGuid();
        EventoId = eventoId;
        TimeId = snapshot.TimeId;
        Ordem = snapshot.Ordem;
        NomeSnapshot = nomeNormalizado;
        TagSnapshot = tagNormalizada;
    }

    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid TimeId { get; private set; }
    public int Ordem { get; private set; }
    public string NomeSnapshot { get; private set; } = string.Empty;
    public string? TagSnapshot { get; private set; }
}
