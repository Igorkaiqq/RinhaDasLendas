using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Series;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Application.Handlers.Series;

public sealed class CreateSeriesCommandHandler(
    ISerieRepository seriesRepository,
    IDraftMontagemRepository draftRepository,
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<CreateSeriesCommand> validator) : IRequestHandler<CreateSeriesCommand, Serie>
{
    public async Task<Serie> Handle(CreateSeriesCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var request = command.Request;
        if (request.Tipo.Value != SerieTipo.DiariaTemporaria)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var activeSeason = await calendarRepository.GetActiveSeasonAsync(cancellationToken)
            ?? throw new DomainException(MessageCodes.ActiveSeasonNotFound);
        if (activeSeason.Id != request.SeasonId || activeSeason.Estado != SeasonEstado.Ativa)
        {
            throw new DomainException(MessageCodes.ActiveSeasonNotFound);
        }

        var competitionId = request.CompeticaoId.Value!.Value;
        var roundId = request.RodadaId.Value!.Value;
        var draftId = request.DraftMontagemId.Value!.Value;
        var dataLocal = request.DataLocal.Value!.Value;
        var competition = await competitionRepository.GetWithRoundsAndRulesAsync(competitionId, cancellationToken)
            ?? throw new DomainException(MessageCodes.CompetitionNotFound);
        if (competition.SeasonId != activeSeason.Id)
        {
            throw new DomainException(MessageCodes.CompetitionSeasonMismatch);
        }

        if (!competition.CircuitoDiario)
        {
            throw new DomainException(MessageCodes.DailyCircuitCompetitionInvalid);
        }

        if (competition.Rodadas.All(round => round.Id != roundId))
        {
            throw new DomainException(MessageCodes.RoundSeasonMismatch);
        }

        var rules = await competitionRepository.GetRulesVersionAsync(request.VersaoRegrasId, cancellationToken)
            ?? throw new DomainException(MessageCodes.RulesVersionNotFound);
        if (rules.SeasonId != activeSeason.Id || rules.CompeticaoId != competition.Id)
        {
            throw new DomainException(MessageCodes.RulesVersionSeasonMismatch);
        }

        var draft = await draftRepository.GetByIdIncludingArchivedAsync(draftId, cancellationToken)
            ?? throw new DomainException(MessageCodes.DraftMontagemNotFound);
        var sides = await BuildDailySidesAsync(draftRepository, draft, request.LadoOrigemIds, cancellationToken);
        if (await seriesRepository.ExistsForDraftAsync(draftId, cancellationToken))
        {
            throw new DomainException(MessageCodes.DailySeriesDraftInvalid);
        }

        await SeriesCommandHandlerSupport.AuthorizeAsync(
            authorization,
            AuthPermissions.CanManageMatches,
            SeriesCommandOperations.Create,
            nameof(Serie),
            SerieTipo.DiariaTemporaria,
            activeSeason.Estado,
            hasStartedSeries: false,
            hasRequiredJustification: false,
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var series = Serie.CriarDiaria(
            activeSeason,
            competition.Id,
            roundId,
            rules.Id,
            draft.Id,
            rules.Formato,
            rules.ModoDraft,
            request.AgendadaPara,
            dataLocal,
            actorId,
            now,
            sides);
        var correlationId = Guid.NewGuid();
        await seriesRepository.AddAsync(series, cancellationToken);
        await SeriesCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            RecursoCompetitivoTipo.Serie,
            series.Id,
            AcaoAuditoriaCompetitiva.SerieCriada,
            actorId,
            AuthPermissions.CanManageMatches,
            justification: null,
            previous: null,
            SeriesCommandHandlerSupport.SeriesSnapshot(series),
            correlationId,
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }

    private static async Task<IReadOnlyCollection<LadoSerieInput>> BuildDailySidesAsync(
        IDraftMontagemRepository draftRepository,
        DraftMontagem draft,
        IReadOnlyCollection<Guid> requestedSideIds,
        CancellationToken cancellationToken)
    {
        if (draft.Status != DraftMontagemStatus.Finalizada)
        {
            throw new DomainException(MessageCodes.DailySeriesDraftInvalid);
        }

        var teams = draft.Times.OrderBy(team => team.Ordem).ToArray();
        if (teams.Length != 2
            || !teams.Select(team => team.Id).Order().SequenceEqual(requestedSideIds.Order()))
        {
            throw new DomainException(MessageCodes.DailySeriesSidesInvalid);
        }

        var participants = draft.Participantes
            .Where(participant => participant.TimeId.HasValue)
            .ToArray();
        var playerIds = participants.Select(participant => participant.JogadorId).Distinct().ToArray();
        var players = await draftRepository.GetJogadoresByIdsAsync(playerIds, cancellationToken);
        var playersById = players.ToDictionary(player => player.Id);
        if (playersById.Count != playerIds.Length)
        {
            throw new DomainException(MessageCodes.DailySeriesSidesInvalid);
        }

        var captainIds = teams.Select(team => team.CapitaoId).ToArray();
        if (captainIds.Any(captainId => !captainId.HasValue)
            || captainIds.Select(captainId => captainId!.Value).Distinct().Count() != 2)
        {
            throw new DomainException(MessageCodes.DailySeriesCaptainsInvalid);
        }

        var result = new List<LadoSerieInput>(2);
        foreach (var team in teams)
        {
            var teamParticipants = participants
                .Where(participant => participant.TimeId == team.Id)
                .OrderBy(participant => participant.Ordem)
                .ToArray();
            if (team.CapitaoId is not Guid captainId
                || teamParticipants.All(participant => participant.JogadorId != captainId)
                || !playersById.TryGetValue(captainId, out var captain)
                || teamParticipants.Length == 0)
            {
                throw new DomainException(MessageCodes.DailySeriesCaptainsInvalid);
            }

            result.Add(new LadoSerieInput(
                team.Ordem,
                LadoSerieTipo.Temporario,
                team.Id,
                team.Nome,
                tagSnapshot: null,
                captainId,
                captain.NomeExibicao,
                teamParticipants.Select(participant => new ParticipanteEsperadoSerieInput(
                    participant.JogadorId,
                    playersById[participant.JogadorId].NomeExibicao,
                    participant.Ordem)).ToArray()));
        }

        return result;
    }
}

public sealed class StartSeriesCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<StartSeriesCommand> validator) : IRequestHandler<StartSeriesCommand, Serie>
{
    public async Task<Serie> Handle(StartSeriesCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await SeriesCommandHandlerSupport.LoadAsync(repository, command.SeriesId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeNormalOperationAsync(authorization, series, cancellationToken);
        var previous = SeriesCommandHandlerSupport.SeriesSnapshot(series);
        var now = timeProvider.GetUtcNow();
        series.Iniciar(actorId, now);
        await SeriesCommandHandlerSupport.StageSeriesAuditAsync(
            auditRepository, series, AcaoAuditoriaCompetitiva.SerieIniciada, actorId,
            AuthPermissions.CanManageMatches, null, previous, Guid.NewGuid(), now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

public sealed class CancelSeriesCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<CancelSeriesCommand> validator) : IRequestHandler<CancelSeriesCommand, Serie>
{
    public async Task<Serie> Handle(CancelSeriesCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await SeriesCommandHandlerSupport.LoadAsync(repository, command.SeriesId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeNormalOperationAsync(authorization, series, cancellationToken);
        var previous = SeriesCommandHandlerSupport.SeriesSnapshot(series);
        var now = timeProvider.GetUtcNow();
        series.Cancelar(actorId, now);
        await SeriesCommandHandlerSupport.StageSeriesAuditAsync(
            auditRepository, series, AcaoAuditoriaCompetitiva.SerieCancelada, actorId,
            AuthPermissions.CanManageMatches, command.Request.Justificativa, previous,
            Guid.NewGuid(), now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

public sealed class AnnulSeriesCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<AnnulSeriesCommand> validator) : IRequestHandler<AnnulSeriesCommand, Serie>
{
    public async Task<Serie> Handle(AnnulSeriesCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await SeriesCommandHandlerSupport.LoadAsync(repository, command.SeriesId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeAsync(
            authorization, AuthPermissions.CanFinalizeMatches, SeriesCommandOperations.TechnicalAnnulment,
            nameof(Serie), series.Tipo, seasonState: null, hasStartedSeries: true,
            hasRequiredJustification: true, cancellationToken);
        var previous = SeriesCommandHandlerSupport.SeriesSnapshot(series);
        var now = timeProvider.GetUtcNow();
        series.Anular(actorId, now);
        await SeriesCommandHandlerSupport.StageSeriesAuditAsync(
            auditRepository, series, AcaoAuditoriaCompetitiva.SerieAnulada, actorId,
            AuthPermissions.CanFinalizeMatches, command.Request.Justificativa, previous,
            Guid.NewGuid(), now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

internal static class SeriesCommandOperations
{
    public const string Create = "CriarSerie";
    public const string NormalOperation = "OperacaoNormal";
    public const string NormalFinalization = "FinalizacaoNormal";
    public const string TechnicalCorrection = "CorrecaoTecnica";
    public const string TechnicalAnnulment = "AnulacaoTecnica";
}

internal static class SeriesCommandHandlerSupport
{
    public static Guid GetActorId(ICurrentActor actor) =>
        actor.UserId is Guid actorId && actorId != Guid.Empty
            ? actorId
            : throw new DomainException(MessageCodes.UnauthorizedAccess);

    public static async Task<Serie> LoadAsync(
        ISerieRepository repository,
        Guid seriesId,
        CancellationToken cancellationToken) =>
        await repository.GetAggregateAsync(seriesId, cancellationToken)
            ?? throw new DomainException(MessageCodes.CompetitiveSeriesNotFound);

    public static void EnsureExpectedVersion(Serie series, long expectedVersion)
    {
        if (series.Versao != expectedVersion)
        {
            throw new DomainException(MessageCodes.CompetitiveResourceVersionStale);
        }
    }

    public static Task AuthorizeNormalOperationAsync(
        ICompetitiveAuthorizationService authorization,
        Serie series,
        CancellationToken cancellationToken) =>
        AuthorizeAsync(
            authorization, AuthPermissions.CanManageMatches, SeriesCommandOperations.NormalOperation,
            nameof(Serie), series.Tipo, seasonState: null,
            hasStartedSeries: series.Estado != SerieEstado.Agendada,
            hasRequiredJustification: false, cancellationToken);

    public static Task AuthorizeNormalFinalizationAsync(
        ICompetitiveAuthorizationService authorization,
        Serie series,
        CancellationToken cancellationToken) =>
        AuthorizeAsync(
            authorization, AuthPermissions.CanFinalizeMatches, SeriesCommandOperations.NormalFinalization,
            nameof(Serie), series.Tipo, seasonState: null, hasStartedSeries: true,
            hasRequiredJustification: false, cancellationToken);

    public static async Task AuthorizeAsync(
        ICompetitiveAuthorizationService authorization,
        string capability,
        string operation,
        string resourceType,
        SerieTipo? seriesType,
        SeasonEstado? seasonState,
        bool hasStartedSeries,
        bool hasRequiredJustification,
        CancellationToken cancellationToken)
    {
        var context = new CompetitiveAuthorizationContext(
            capability,
            operation,
            resourceType,
            seriesType?.ToString(),
            seasonState?.ToString(),
            hasStartedSeries,
            hasRequiredJustification);
        if (!await authorization.AuthorizeAsync(context, cancellationToken))
        {
            throw new DomainException(MessageCodes.CompetitiveAccessDenied);
        }
    }

    public static SnapshotAuditoriaRedigido SeriesSnapshot(Serie series) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = series.Id,
            [CampoSnapshotAuditoria.SeasonId] = series.SeasonId,
            [CampoSnapshotAuditoria.CompeticaoId] = series.CompeticaoId,
            [CampoSnapshotAuditoria.RodadaId] = series.RodadaId,
            [CampoSnapshotAuditoria.VersaoRegrasId] = series.VersaoRegrasId,
            [CampoSnapshotAuditoria.EventoId] = series.EventoId,
            [CampoSnapshotAuditoria.DraftMontagemId] = series.DraftMontagemId,
            [CampoSnapshotAuditoria.TipoSerie] = series.Tipo,
            [CampoSnapshotAuditoria.FormatoSerie] = series.Formato,
            [CampoSnapshotAuditoria.ModoDraft] = series.ModoDraft,
            [CampoSnapshotAuditoria.FearlessHabilitado] = series.FearlessHabilitado,
            [CampoSnapshotAuditoria.EstadoSerie] = series.Estado,
            [CampoSnapshotAuditoria.AgendadaPara] = series.AgendadaPara,
            [CampoSnapshotAuditoria.DataLocal] = series.DataLocal,
            [CampoSnapshotAuditoria.LadoVencedorId] = series.LadoVencedorId,
            [CampoSnapshotAuditoria.Resultado] = new[]
            {
                series.Resultado.VitoriasLadoUm,
                series.Resultado.VitoriasLadoDois,
            },
            [CampoSnapshotAuditoria.RevisaoNecessaria] = series.RevisaoNecessaria,
            [CampoSnapshotAuditoria.Versao] = series.Versao,
        });

    public static SnapshotAuditoriaRedigido MatchSnapshot(Partida match) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = match.Id,
            [CampoSnapshotAuditoria.SerieId] = match.SerieId,
            [CampoSnapshotAuditoria.Ordem] = match.Ordem,
            [CampoSnapshotAuditoria.EstadoPartida] = match.Estado,
            [CampoSnapshotAuditoria.LadoVencedorId] = match.LadoVencedorId,
            [CampoSnapshotAuditoria.MotivoTerminoPartida] = match.MotivoTermino,
            [CampoSnapshotAuditoria.DecisaoPicksRemake] = match.DecisaoPicksRemake,
            [CampoSnapshotAuditoria.Picks] = match.Picks.Where(pick => pick.Valido)
                .OrderBy(pick => pick.LadoSerieId).ThenBy(pick => pick.Ordem)
                .Select(pick => pick.ChampionId).ToArray(),
            [CampoSnapshotAuditoria.RevisaoNecessaria] = match.ConflitoFearless,
            [CampoSnapshotAuditoria.Versao] = match.Versao,
        });

    public static Task StageSeriesAuditAsync(
        ICompetitiveAuditRepository auditRepository,
        Serie series,
        AcaoAuditoriaCompetitiva action,
        Guid actorId,
        string capability,
        string? justification,
        SnapshotAuditoriaRedigido previous,
        Guid correlationId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        AddAuditAsync(
            auditRepository, RecursoCompetitivoTipo.Serie, series.Id, action, actorId,
            capability, justification, previous, SeriesSnapshot(series), correlationId,
            occurredAt, cancellationToken);

    public static Task AddAuditAsync(
        ICompetitiveAuditRepository auditRepository,
        RecursoCompetitivoTipo resourceType,
        Guid resourceId,
        AcaoAuditoriaCompetitiva action,
        Guid actorId,
        string capability,
        string? justification,
        SnapshotAuditoriaRedigido? previous,
        SnapshotAuditoriaRedigido? current,
        Guid correlationId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        auditRepository.AddAsync(
            new RegistroAuditoriaCompetitiva(
                resourceType, resourceId, action, actorId, capability, justification,
                previous, current, correlationId, occurredAt),
            cancellationToken);
}
