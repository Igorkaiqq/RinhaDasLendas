using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Serie
{
    private static readonly TimeZoneInfo SaoPauloTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly List<LadoSerie> _lados = [];
    private readonly List<Partida> _partidas = [];

    private Serie()
    {
    }

    public Serie(
        Guid seasonId,
        Guid? competicaoId,
        Guid? rodadaId,
        Guid versaoRegrasId,
        Guid? eventoId,
        Guid? draftMontagemId,
        SerieTipo tipo,
        SerieFormato formato,
        ModoDraft modoDraft,
        bool fearlessHabilitado,
        DateTimeOffset agendadaPara,
        DateOnly? dataLocal,
        Guid criadaPorUsuarioId,
        DateTimeOffset criadaEm,
        IReadOnlyCollection<LadoSerie> lados)
    {
        ValidarPrimitivos(
            seasonId,
            competicaoId,
            rodadaId,
            versaoRegrasId,
            eventoId,
            draftMontagemId,
            tipo,
            formato,
            modoDraft,
            fearlessHabilitado,
            criadaPorUsuarioId);
        ValidarLados(lados);
        ValidarContexto(
            tipo,
            competicaoId,
            rodadaId,
            eventoId,
            draftMontagemId,
            modoDraft,
            fearlessHabilitado,
            agendadaPara,
            dataLocal,
            lados);

        var criadaEmUtc = criadaEm.ToUniversalTime();

        Id = Guid.NewGuid();
        SeasonId = seasonId;
        CompeticaoId = competicaoId;
        RodadaId = rodadaId;
        VersaoRegrasId = versaoRegrasId;
        EventoId = eventoId;
        DraftMontagemId = draftMontagemId;
        Tipo = tipo;
        Formato = formato;
        ModoDraft = modoDraft;
        FearlessHabilitado = fearlessHabilitado;
        Estado = SerieEstado.Agendada;
        AgendadaPara = agendadaPara.ToUniversalTime();
        DataLocal = dataLocal;
        CriadaEm = criadaEmUtc;
        AtualizadaEm = criadaEmUtc;
        CriadaPorUsuarioId = criadaPorUsuarioId;
        AtualizadaPorUsuarioId = criadaPorUsuarioId;

        foreach (var lado in lados.OrderBy(lado => lado.Ordem))
        {
            _lados.Add(lado.CloneFor(Id));
        }
    }

    public Guid Id { get; private set; }
    public Guid SeasonId { get; private set; }
    public Guid? CompeticaoId { get; private set; }
    public Guid? RodadaId { get; private set; }
    public Guid VersaoRegrasId { get; private set; }
    public Guid? EventoId { get; private set; }
    public Guid? DraftMontagemId { get; private set; }
    public SerieTipo Tipo { get; private set; }
    public SerieFormato Formato { get; private set; }
    public ModoDraft ModoDraft { get; private set; }
    public bool FearlessHabilitado { get; private set; }
    public SerieEstado Estado { get; private set; }
    public DateTimeOffset AgendadaPara { get; private set; }
    public DateOnly? DataLocal { get; private set; }
    public Guid? LadoVencedorId { get; private set; }
    public bool RevisaoNecessaria { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }
    public DateTimeOffset? ConcluidaEm { get; private set; }
    public Guid CriadaPorUsuarioId { get; private set; }
    public Guid AtualizadaPorUsuarioId { get; private set; }
    public IReadOnlyCollection<LadoSerie> Lados => _lados.AsReadOnly();

    // T041 adicionara o ciclo controlado de criacao de Partidas pelo agregado.
    public IReadOnlyCollection<Partida> Partidas => _partidas.AsReadOnly();

    private static void ValidarPrimitivos(
        Guid seasonId,
        Guid? competicaoId,
        Guid? rodadaId,
        Guid versaoRegrasId,
        Guid? eventoId,
        Guid? draftMontagemId,
        SerieTipo tipo,
        SerieFormato formato,
        ModoDraft modoDraft,
        bool fearlessHabilitado,
        Guid criadaPorUsuarioId)
    {
        if (seasonId == Guid.Empty
            || versaoRegrasId == Guid.Empty
            || criadaPorUsuarioId == Guid.Empty
            || competicaoId == Guid.Empty
            || rodadaId == Guid.Empty
            || eventoId == Guid.Empty
            || draftMontagemId == Guid.Empty
            || !Enum.IsDefined(tipo)
            || !Enum.IsDefined(modoDraft))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (!Enum.IsDefined(formato))
        {
            throw new DomainException(MessageCodes.SeriesMustBeBestOfThreeOrFive);
        }

        if (eventoId.HasValue && (modoDraft != ModoDraft.Padrao || fearlessHabilitado))
        {
            throw new DomainException(MessageCodes.EventFearlessConflict);
        }

        if (fearlessHabilitado != (modoDraft == ModoDraft.Fearless))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private static void ValidarLados(IReadOnlyCollection<LadoSerie> lados)
    {
        if (lados is null || lados.Count != 2)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        foreach (var lado in lados)
        {
            lado.ValidarEstrutura();
        }

        if (lados.Select(lado => lado.Id).Distinct().Count() != 2
            || lados.Select(lado => lado.OrigemId).Distinct().Count() != 2
            || lados.Select(lado => lado.Ordem).Order().SequenceEqual([1, 2]) is false
            || lados.SelectMany(lado => lado.ParticipantesEsperados)
                .Select(participante => participante.Id)
                .Distinct()
                .Count() != lados.Sum(lado => lado.ParticipantesEsperados.Count)
            || lados.SelectMany(lado => lado.ParticipantesEsperados)
                .Select(participante => participante.JogadorId)
                .Distinct()
                .Count() != lados.Sum(lado => lado.ParticipantesEsperados.Count))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private static void ValidarContexto(
        SerieTipo tipo,
        Guid? competicaoId,
        Guid? rodadaId,
        Guid? eventoId,
        Guid? draftMontagemId,
        ModoDraft modoDraft,
        bool fearlessHabilitado,
        DateTimeOffset agendadaPara,
        DateOnly? dataLocal,
        IReadOnlyCollection<LadoSerie> lados)
    {
        if (eventoId.HasValue && lados.Any(lado => lado.Tipo != LadoSerieTipo.TimeOficial))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        switch (tipo)
        {
            case SerieTipo.DiariaTemporaria:
                ValidarDiaria(
                    competicaoId,
                    rodadaId,
                    draftMontagemId,
                    agendadaPara,
                    dataLocal,
                    lados);
                break;
            case SerieTipo.ConfrontoOficial:
                if (!competicaoId.HasValue
                    || !rodadaId.HasValue
                    || lados.Any(lado => lado.Tipo != LadoSerieTipo.TimeOficial))
                {
                    throw new DomainException(MessageCodes.ValidationError);
                }

                if (draftMontagemId.HasValue || dataLocal.HasValue)
                {
                    throw new DomainException(MessageCodes.ValidationError);
                }

                break;
            case SerieTipo.Amistoso:
                if (competicaoId.HasValue != rodadaId.HasValue
                    || lados.Select(lado => lado.Tipo).Distinct().Count() != 1
                    || draftMontagemId.HasValue
                    || dataLocal.HasValue)
                {
                    throw new DomainException(MessageCodes.ValidationError);
                }

                break;
            default:
                throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private static void ValidarDiaria(
        Guid? competicaoId,
        Guid? rodadaId,
        Guid? draftMontagemId,
        DateTimeOffset agendadaPara,
        DateOnly? dataLocal,
        IReadOnlyCollection<LadoSerie> lados)
    {
        if (!competicaoId.HasValue || !rodadaId.HasValue)
        {
            throw new DomainException(MessageCodes.DailyCircuitCompetitionInvalid);
        }

        if (!draftMontagemId.HasValue)
        {
            throw new DomainException(MessageCodes.DailySeriesDraftInvalid);
        }

        if (!dataLocal.HasValue
            || dataLocal.Value != DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(agendadaPara, SaoPauloTimeZone).DateTime))
        {
            throw new DomainException(MessageCodes.DailySeriesDateInvalid);
        }

        if (lados.Any(lado => lado.Tipo != LadoSerieTipo.Temporario))
        {
            throw new DomainException(MessageCodes.DailySeriesSidesInvalid);
        }

        if (lados.Any(lado => !lado.CapitaoJogadorId.HasValue
            || string.IsNullOrWhiteSpace(lado.CapitaoNomeSnapshot))
            || lados.Select(lado => lado.CapitaoJogadorId).Distinct().Count() != 2)
        {
            throw new DomainException(MessageCodes.DailySeriesCaptainsInvalid);
        }

        if (lados.Any(lado => lado.ParticipantesEsperados.Count == 0))
        {
            throw new DomainException(MessageCodes.DailySeriesSidesInvalid);
        }
    }
}
