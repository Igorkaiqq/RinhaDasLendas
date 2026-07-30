using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class VersaoRegras
{
    private VersaoRegras()
    {
    }

    public VersaoRegras(
        Guid seasonId,
        Guid? competicaoId,
        int numero,
        SerieFormato formato,
        ModoDraft modoDraft,
        Guid publicadaPorUsuarioId,
        DateTimeOffset publicadaEm)
    {
        if (seasonId == Guid.Empty
            || competicaoId == Guid.Empty
            || publicadaPorUsuarioId == Guid.Empty
            || numero <= 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (!Enum.IsDefined(formato))
        {
            throw new DomainException(MessageCodes.SeriesMustBeBestOfThreeOrFive);
        }

        if (!Enum.IsDefined(modoDraft))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Id = Guid.NewGuid();
        SeasonId = seasonId;
        CompeticaoId = competicaoId;
        Numero = numero;
        Formato = formato;
        ModoDraft = modoDraft;
        PublicadaPorUsuarioId = publicadaPorUsuarioId;
        PublicadaEm = publicadaEm.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid SeasonId { get; private set; }
    public Guid? CompeticaoId { get; private set; }
    public int Numero { get; private set; }
    public SerieFormato Formato { get; private set; }
    public ModoDraft ModoDraft { get; private set; }
    public DateTimeOffset PublicadaEm { get; private set; }
    public Guid PublicadaPorUsuarioId { get; private set; }
}
