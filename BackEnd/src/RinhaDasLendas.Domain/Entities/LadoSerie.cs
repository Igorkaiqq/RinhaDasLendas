using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class LadoSerie
{
    private readonly List<ParticipanteEsperadoSerie> _participantesEsperados = [];

    private LadoSerie()
    {
    }

    internal LadoSerie(
        Guid serieId,
        int ordem,
        LadoSerieTipo tipo,
        Guid origemId,
        string nomeSnapshot,
        string? tagSnapshot,
        Guid? capitaoJogadorId,
        string? capitaoNomeSnapshot,
        IReadOnlyCollection<ParticipanteEsperadoSerie> participantesEsperados)
    {
        Validar(
            serieId,
            ordem,
            tipo,
            origemId,
            nomeSnapshot,
            tagSnapshot,
            capitaoJogadorId,
            capitaoNomeSnapshot,
            participantesEsperados);

        Id = Guid.NewGuid();
        SerieId = serieId;
        Ordem = ordem;
        Tipo = tipo;
        OrigemId = origemId;
        NomeSnapshot = nomeSnapshot.Trim();
        TagSnapshot = NormalizarOpcional(tagSnapshot);
        CapitaoJogadorId = capitaoJogadorId;
        CapitaoNomeSnapshot = NormalizarOpcional(capitaoNomeSnapshot);

        foreach (var participante in participantesEsperados)
        {
            _participantesEsperados.Add(participante.CloneFor(Id));
        }
    }

    private LadoSerie(LadoSerie origem, Guid serieId)
    {
        origem.ValidarEstrutura();

        Id = Guid.NewGuid();
        SerieId = serieId;
        Ordem = origem.Ordem;
        Tipo = origem.Tipo;
        OrigemId = origem.OrigemId;
        NomeSnapshot = origem.NomeSnapshot;
        TagSnapshot = origem.TagSnapshot;
        CapitaoJogadorId = origem.CapitaoJogadorId;
        CapitaoNomeSnapshot = origem.CapitaoNomeSnapshot;

        foreach (var participante in origem.ParticipantesEsperados)
        {
            _participantesEsperados.Add(participante.CloneFor(Id));
        }
    }

    public Guid Id { get; private set; }
    public Guid SerieId { get; private set; }
    public int Ordem { get; private set; }
    public LadoSerieTipo Tipo { get; private set; }
    public Guid OrigemId { get; private set; }
    public string NomeSnapshot { get; private set; } = string.Empty;
    public string? TagSnapshot { get; private set; }
    public Guid? CapitaoJogadorId { get; private set; }
    public string? CapitaoNomeSnapshot { get; private set; }
    public IReadOnlyCollection<ParticipanteEsperadoSerie> ParticipantesEsperados =>
        _participantesEsperados.AsReadOnly();

    internal LadoSerie CloneFor(Guid serieId) => new(this, serieId);

    internal void ValidarEstrutura()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Validar(
            SerieId,
            Ordem,
            Tipo,
            OrigemId,
            NomeSnapshot,
            TagSnapshot,
            CapitaoJogadorId,
            CapitaoNomeSnapshot,
            ParticipantesEsperados);
    }

    private static void Validar(
        Guid serieId,
        int ordem,
        LadoSerieTipo tipo,
        Guid origemId,
        string nomeSnapshot,
        string? tagSnapshot,
        Guid? capitaoJogadorId,
        string? capitaoNomeSnapshot,
        IReadOnlyCollection<ParticipanteEsperadoSerie> participantesEsperados)
    {
        if (serieId == Guid.Empty
            || origemId == Guid.Empty
            || ordem is < 1 or > 2
            || !Enum.IsDefined(tipo))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nomeSnapshot))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        if (nomeSnapshot.Trim().Length > 100
            || NormalizarOpcional(tagSnapshot)?.Length > 10
            || NormalizarOpcional(capitaoNomeSnapshot)?.Length > 100)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        if (capitaoJogadorId == Guid.Empty
            || capitaoJogadorId.HasValue != !string.IsNullOrWhiteSpace(capitaoNomeSnapshot)
            || participantesEsperados is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        foreach (var participante in participantesEsperados)
        {
            participante.ValidarEstrutura();
        }

        if (participantesEsperados.Select(participante => participante.Id).Distinct().Count() != participantesEsperados.Count
            || participantesEsperados.Select(participante => participante.JogadorId).Distinct().Count() != participantesEsperados.Count
            || participantesEsperados.Select(participante => participante.Ordem).Distinct().Count() != participantesEsperados.Count)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
