using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Rules;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Serie
{
    private readonly List<LadoSerie> _lados = [];
    private readonly List<Partida> _partidas = [];

    private Serie()
    {
    }

    internal Serie(
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
    public IReadOnlyCollection<Partida> Partidas => _partidas.AsReadOnly();
    public ResultadoSerie Resultado => SerieRules.CalcularResultado(Formato, Lados, Partidas);

    public static Serie CriarDiaria(
        Season season,
        Guid competicaoId,
        Guid rodadaId,
        Guid versaoRegrasId,
        Guid draftMontagemId,
        SerieFormato formato,
        ModoDraft modoDraft,
        DateTimeOffset agendadaPara,
        DateOnly dataLocal,
        Guid criadaPorUsuarioId,
        DateTimeOffset criadaEm,
        IReadOnlyCollection<LadoSerieInput> lados)
    {
        if (season is null || lados is null)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        SerieRules.ValidarElegibilidadeDiaria(
            agendadaPara,
            dataLocal,
            season.DataInicio,
            season.DataFimExclusiva);
        var templates = lados.Select(lado => new LadoSerie(
            Guid.NewGuid(),
            lado.Ordem,
            lado.Tipo,
            lado.OrigemId,
            lado.NomeSnapshot,
            lado.TagSnapshot,
            lado.CapitaoJogadorId,
            lado.CapitaoNomeSnapshot,
            lado.ParticipantesEsperados.Select(participante => new ParticipanteEsperadoSerie(
                Guid.NewGuid(),
                participante.JogadorId,
                participante.NomeSnapshot,
                participante.Ordem)).ToArray())).ToArray();

        return new Serie(
            season.Id,
            competicaoId,
            rodadaId,
            versaoRegrasId,
            eventoId: null,
            draftMontagemId,
            SerieTipo.DiariaTemporaria,
            formato,
            modoDraft,
            fearlessHabilitado: modoDraft == ModoDraft.Fearless,
            agendadaPara,
            dataLocal,
            criadaPorUsuarioId,
            criadaEm,
            templates);
    }

    public void Iniciar(Guid usuarioId, DateTimeOffset atualizadoEm)
    {
        ValidarAtor(usuarioId);
        if (Estado != SerieEstado.Agendada)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        Estado = SerieEstado.EmAndamento;
        Atualizar(usuarioId, atualizadoEm);
    }

    public void Cancelar(Guid usuarioId, DateTimeOffset atualizadoEm)
    {
        ValidarAtor(usuarioId);
        if (Estado is not (SerieEstado.Agendada or SerieEstado.EmAndamento))
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        Estado = SerieEstado.Cancelada;
        LadoVencedorId = null;
        ConcluidaEm = null;
        Atualizar(usuarioId, atualizadoEm);
    }

    public void Anular(Guid usuarioId, DateTimeOffset atualizadoEm)
    {
        ValidarAtor(usuarioId);
        if (Estado is SerieEstado.Cancelada or SerieEstado.Anulada)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        Estado = SerieEstado.Anulada;
        LadoVencedorId = null;
        ConcluidaEm = null;
        Atualizar(usuarioId, atualizadoEm);
    }

    public Partida AdicionarPartida(Guid usuarioId, DateTimeOffset criadaEm)
    {
        ValidarAtor(usuarioId);
        if (Estado == SerieEstado.Concluida || Resultado.LadoVencedorId.HasValue)
        {
            throw new DomainException(MessageCodes.SeriesAlreadyDecided);
        }

        if (Estado != SerieEstado.EmAndamento)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        var partida = new Partida(Id, _partidas.Count == 0 ? 1 : _partidas.Max(item => item.Ordem) + 1, criadaEm);
        _partidas.Add(partida);
        Atualizar(usuarioId, criadaEm);
        return partida;
    }

    public void RegistrarPicks(
        Guid partidaId,
        IReadOnlyCollection<(Guid LadoSerieId, int ChampionId, int Ordem)> picks,
        Guid usuarioId,
        DateTimeOffset atualizadoEm)
    {
        ValidarOperacaoNormal(usuarioId);
        var partida = ObterPartida(partidaId);
        FearlessRules.ValidarPicks(Lados, picks);

        if (FearlessHabilitado)
        {
            var bloqueios = ObterBloqueiosFearless(partida.Ordem);
            if (picks.Any(pick => bloqueios.Contains(pick.ChampionId)))
            {
                throw new DomainException(MessageCodes.ChampionBlockedByFearless);
            }
        }

        partida.RegistrarPicks(picks, atualizadoEm);
        ReconstruirConflitosFearless(atualizadoEm, partida.Id);
        Atualizar(usuarioId, atualizadoEm);
    }

    public void ConfirmarPartida(
        Guid partidaId,
        Guid ladoVencedorId,
        MotivoTerminoPartida motivoTermino,
        Guid usuarioId,
        DateTimeOffset confirmadoEm)
    {
        ValidarAtor(usuarioId);
        if (Estado == SerieEstado.Concluida || Resultado.LadoVencedorId.HasValue)
        {
            throw new DomainException(MessageCodes.SeriesAlreadyDecided);
        }

        if (Estado != SerieEstado.EmAndamento)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }

        if (RevisaoNecessaria)
        {
            throw new DomainException(MessageCodes.SeriesReviewRequired);
        }

        var partida = ObterPartida(partidaId);
        ValidarLado(ladoVencedorId);
        var picksValidos = partida.Picks.Where(pick => pick.Valido).ToArray();
        FearlessRules.ValidarPicks(
            Lados,
            picksValidos.Select(pick => (pick.LadoSerieId, pick.ChampionId, pick.Ordem)).ToArray());
        if (FearlessHabilitado)
        {
            var bloqueios = ObterBloqueiosFearless(partida.Ordem);
            if (picksValidos.Any(pick => bloqueios.Contains(pick.ChampionId)))
            {
                throw new DomainException(MessageCodes.ChampionBlockedByFearless);
            }
        }

        partida.Confirmar(ladoVencedorId, motivoTermino, confirmadoEm);
        AplicarResultadoDerivado(confirmadoEm);
        Atualizar(usuarioId, confirmadoEm);
    }

    public void MarcarPartidaComoRemake(
        Guid partidaId,
        DecisaoPicksRemake decisao,
        Guid usuarioId,
        DateTimeOffset atualizadoEm)
    {
        ValidarOperacaoNormal(usuarioId);
        var partida = ObterPartida(partidaId);
        if (!Enum.IsDefined(decisao))
        {
            throw new DomainException(MessageCodes.RemakeDecisionRequired);
        }

        if (decisao == DecisaoPicksRemake.PreservarPicks)
        {
            var picksValidos = partida.Picks.Where(pick => pick.Valido)
                .Select(pick => (pick.LadoSerieId, pick.ChampionId, pick.Ordem))
                .ToArray();
            FearlessRules.ValidarPicks(Lados, picksValidos);
        }

        partida.MarcarComoRemake(decisao, atualizadoEm);
        ReconstruirConflitosFearless(atualizadoEm, partida.Id);
        Atualizar(usuarioId, atualizadoEm);
    }

    public void CorrigirPicks(
        Guid partidaId,
        IReadOnlyCollection<(Guid LadoSerieId, int ChampionId, int Ordem)> picks,
        Guid usuarioId,
        DateTimeOffset atualizadoEm)
    {
        ValidarCorrecao(usuarioId);
        var partida = ObterPartida(partidaId);
        FearlessRules.ValidarPicks(Lados, picks);
        partida.CorrigirPicks(picks, atualizadoEm);
        ReconstruirConflitosFearless(atualizadoEm, partida.Id);
        Atualizar(usuarioId, atualizadoEm);
    }

    public void CorrigirResultadoPartida(
        Guid partidaId,
        Guid ladoVencedorId,
        MotivoTerminoPartida motivoTermino,
        bool anularSerieSeInconclusiva,
        Guid usuarioId,
        DateTimeOffset atualizadoEm)
    {
        ValidarCorrecao(usuarioId);
        var partida = ObterPartida(partidaId);
        ValidarLado(ladoVencedorId);
        ValidarCorrecaoDeSerieConcluida(partida, ladoVencedorId, anularSerieSeInconclusiva);

        partida.CorrigirResultado(ladoVencedorId, motivoTermino, atualizadoEm);
        ReconstruirEstadoAposCorrecao(anularSerieSeInconclusiva, atualizadoEm);
        ReconstruirConflitosFearless(atualizadoEm, partida.Id);
        Atualizar(usuarioId, atualizadoEm);
    }

    public void AnularPartida(Guid partidaId, Guid usuarioId, DateTimeOffset atualizadoEm) =>
        AnularPartida(partidaId, false, usuarioId, atualizadoEm);

    public void AnularPartida(
        Guid partidaId,
        bool anularSerieSeInconclusiva,
        Guid usuarioId,
        DateTimeOffset atualizadoEm)
    {
        ValidarCorrecao(usuarioId);
        var partida = ObterPartida(partidaId);
        ValidarCorrecaoDeSerieConcluida(partida, null, anularSerieSeInconclusiva);

        partida.Anular(atualizadoEm);
        ReconstruirEstadoAposCorrecao(anularSerieSeInconclusiva, atualizadoEm);
        ReconstruirConflitosFearless(atualizadoEm, partida.Id);
        Atualizar(usuarioId, atualizadoEm);
    }

    public IReadOnlySet<int> ObterBloqueiosFearless(int ordemPartida) =>
        FearlessHabilitado
            ? FearlessRules.ReconstruirBloqueios(Partidas, ordemPartida)
            : new HashSet<int>();

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
            || dataLocal.Value != SerieRules.ObterDataCompetitiva(agendadaPara))
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

    private void ValidarOperacaoNormal(Guid usuarioId)
    {
        ValidarAtor(usuarioId);
        if (Estado != SerieEstado.EmAndamento)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }
    }

    private void ValidarCorrecao(Guid usuarioId)
    {
        ValidarAtor(usuarioId);
        if (Estado is SerieEstado.Cancelada or SerieEstado.Anulada or SerieEstado.Agendada)
        {
            throw new DomainException(MessageCodes.SeriesTransitionInvalid);
        }
    }

    private static void ValidarAtor(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private Partida ObterPartida(Guid partidaId)
    {
        if (partidaId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return _partidas.SingleOrDefault(partida => partida.Id == partidaId)
            ?? throw new DomainException(MessageCodes.CompetitiveMatchNotFound);
    }

    private void ValidarLado(Guid ladoId)
    {
        if (ladoId == Guid.Empty || _lados.All(lado => lado.Id != ladoId))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private void AplicarResultadoDerivado(DateTimeOffset atualizadoEm)
    {
        var resultado = Resultado;
        LadoVencedorId = resultado.LadoVencedorId;
        if (resultado.LadoVencedorId.HasValue)
        {
            Estado = SerieEstado.Concluida;
            ConcluidaEm = atualizadoEm.ToUniversalTime();
        }
    }

    private void ValidarCorrecaoDeSerieConcluida(
        Partida partida,
        Guid? novoVencedorId,
        bool anularSerieSeInconclusiva)
    {
        if (Estado != SerieEstado.Concluida || partida.Estado != PartidaEstado.Confirmada)
        {
            return;
        }

        var vitoriasNecessarias = SerieRules.ObterVitoriasNecessarias(Formato);
        var confirmadas = _partidas.Where(item => item.Estado == PartidaEstado.Confirmada && item.Id != partida.Id);
        var existeVencedor = _lados.Any(lado =>
            confirmadas.Count(item => item.LadoVencedorId == lado.Id)
            + (novoVencedorId == lado.Id ? 1 : 0) >= vitoriasNecessarias);
        if (!existeVencedor && !anularSerieSeInconclusiva)
        {
            throw new DomainException(MessageCodes.CorrectionAnnulConfirmationRequired);
        }
    }

    private void ReconstruirEstadoAposCorrecao(
        bool anularSerieSeInconclusiva,
        DateTimeOffset atualizadoEm)
    {
        var resultado = Resultado;
        LadoVencedorId = resultado.LadoVencedorId;
        if (Estado == SerieEstado.EmAndamento)
        {
            if (resultado.LadoVencedorId.HasValue)
            {
                Estado = SerieEstado.Concluida;
                ConcluidaEm = atualizadoEm.ToUniversalTime();
            }

            return;
        }

        if (resultado.LadoVencedorId.HasValue)
        {
            return;
        }

        if (anularSerieSeInconclusiva)
        {
            Estado = SerieEstado.Anulada;
            LadoVencedorId = null;
            ConcluidaEm = null;
        }
    }

    private void ReconstruirConflitosFearless(DateTimeOffset atualizadoEm, Guid partidaJaAtualizadaId)
    {
        if (!FearlessHabilitado)
        {
            foreach (var partida in _partidas)
            {
                partida.DefinirConflitoFearless(false, atualizadoEm, partida.Id != partidaJaAtualizadaId);
            }

            RevisaoNecessaria = false;
            return;
        }

        var bloqueios = new HashSet<int>();
        foreach (var partida in _partidas.OrderBy(item => item.Ordem))
        {
            partida.DefinirConflitoFearless(
                FearlessRules.PossuiConflito(partida, bloqueios),
                atualizadoEm,
                partida.Id != partidaJaAtualizadaId);
            if (partida.Estado == PartidaEstado.Confirmada
                || partida.Estado == PartidaEstado.Remake
                    && partida.DecisaoPicksRemake == DecisaoPicksRemake.PreservarPicks)
            {
                bloqueios.UnionWith(partida.Picks.Where(pick => pick.Valido).Select(pick => pick.ChampionId));
            }
        }

        RevisaoNecessaria = _partidas.Any(partida => partida.ConflitoFearless);
    }

    private void Atualizar(Guid usuarioId, DateTimeOffset atualizadoEm)
    {
        Versao++;
        AtualizadaEm = atualizadoEm.ToUniversalTime();
        AtualizadaPorUsuarioId = usuarioId;
    }
}
