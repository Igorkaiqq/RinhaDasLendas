using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Partidas;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Series;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Application.Handlers.Partidas;

public sealed class AddNextMatchCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<AddNextMatchCommand> validator) : IRequestHandler<AddNextMatchCommand, Partida>
{
    public async Task<Partida> Handle(AddNextMatchCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await MatchCommandHandlerSupport.LoadSeriesAsync(repository, command.SeriesId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeNormalOperationAsync(authorization, series, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var match = series.AdicionarPartida(actorId, now);
        await SeriesCommandHandlerSupport.AddAuditAsync(
            auditRepository, RecursoCompetitivoTipo.Partida, match.Id,
            AcaoAuditoriaCompetitiva.PartidaAdicionada, actorId,
            AuthPermissions.CanManageMatches, null, null,
            SeriesCommandHandlerSupport.MatchSnapshot(match), Guid.NewGuid(), now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return match;
    }
}

public sealed class RegisterMatchPicksCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<RegisterMatchPicksCommand> validator) : IRequestHandler<RegisterMatchPicksCommand, Serie>
{
    public async Task<Serie> Handle(RegisterMatchPicksCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await MatchCommandHandlerSupport.LoadSeriesByMatchAsync(repository, command.MatchId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeNormalOperationAsync(authorization, series, cancellationToken);
        var match = MatchCommandHandlerSupport.GetMatch(series, command.MatchId);
        var previous = SeriesCommandHandlerSupport.MatchSnapshot(match);
        var now = timeProvider.GetUtcNow();
        series.RegistrarPicks(command.MatchId, MatchCommandHandlerSupport.MapPicks(command.Request.Lados), actorId, now);
        await MatchCommandHandlerSupport.StageMatchAuditAsync(
            auditRepository, match, AcaoAuditoriaCompetitiva.PicksPartidaRegistrados,
            actorId, AuthPermissions.CanManageMatches, null, previous, Guid.NewGuid(), now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

public sealed class ConfirmMatchResultCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<ConfirmMatchResultCommand> validator) : IRequestHandler<ConfirmMatchResultCommand, Serie>
{
    public async Task<Serie> Handle(ConfirmMatchResultCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await MatchCommandHandlerSupport.LoadSeriesByMatchAsync(repository, command.MatchId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeNormalFinalizationAsync(authorization, series, cancellationToken);
        var match = MatchCommandHandlerSupport.GetMatch(series, command.MatchId);
        var previousMatch = SeriesCommandHandlerSupport.MatchSnapshot(match);
        var previousSeries = SeriesCommandHandlerSupport.SeriesSnapshot(series);
        var now = timeProvider.GetUtcNow();
        series.ConfirmarPartida(
            command.MatchId,
            command.Request.LadoVencedorId,
            command.Request.MotivoTermino.Value!.Value,
            actorId,
            now);
        var correlationId = Guid.NewGuid();
        await MatchCommandHandlerSupport.StageMatchAuditAsync(
            auditRepository, match, AcaoAuditoriaCompetitiva.PartidaConfirmada,
            actorId, AuthPermissions.CanFinalizeMatches, null, previousMatch,
            correlationId, now, cancellationToken);
        if (series.Estado == SerieEstado.Concluida)
        {
            await SeriesCommandHandlerSupport.StageSeriesAuditAsync(
                auditRepository, series, AcaoAuditoriaCompetitiva.ResultadoSerieConfirmado,
                actorId, AuthPermissions.CanFinalizeMatches, null, previousSeries,
                correlationId, now, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

public sealed class MarkMatchRemakeCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<MarkMatchRemakeCommand> validator) : IRequestHandler<MarkMatchRemakeCommand, Serie>
{
    public async Task<Serie> Handle(MarkMatchRemakeCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await MatchCommandHandlerSupport.LoadSeriesByMatchAsync(repository, command.MatchId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await SeriesCommandHandlerSupport.AuthorizeNormalOperationAsync(authorization, series, cancellationToken);
        var match = MatchCommandHandlerSupport.GetMatch(series, command.MatchId);
        var previous = SeriesCommandHandlerSupport.MatchSnapshot(match);
        var now = timeProvider.GetUtcNow();
        series.MarcarPartidaComoRemake(
            command.MatchId,
            command.Request.DecisaoPicks.Value!.Value,
            actorId,
            now);
        await MatchCommandHandlerSupport.StageMatchAuditAsync(
            auditRepository, match, AcaoAuditoriaCompetitiva.PartidaMarcadaComoRemake,
            actorId, AuthPermissions.CanManageMatches, command.Request.Justificativa,
            previous, Guid.NewGuid(), now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

public sealed class AnnulMatchCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<AnnulMatchCommand> validator) : IRequestHandler<AnnulMatchCommand, Serie>
{
    public async Task<Serie> Handle(AnnulMatchCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await MatchCommandHandlerSupport.LoadSeriesByMatchAsync(repository, command.MatchId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await MatchCommandHandlerSupport.AuthorizeTechnicalAsync(
            authorization, series, SeriesCommandOperations.TechnicalAnnulment, cancellationToken);
        var match = MatchCommandHandlerSupport.GetMatch(series, command.MatchId);
        var previousMatch = SeriesCommandHandlerSupport.MatchSnapshot(match);
        var previousSeries = SeriesCommandHandlerSupport.SeriesSnapshot(series);
        var previousState = series.Estado;
        var now = timeProvider.GetUtcNow();
        series.AnularPartida(
            command.MatchId,
            command.Request.AnularSerieSeInconclusiva.Value!.Value,
            actorId,
            now);
        var correlationId = Guid.NewGuid();
        await MatchCommandHandlerSupport.StageMatchAuditAsync(
            auditRepository, match, AcaoAuditoriaCompetitiva.PartidaAnulada,
            actorId, AuthPermissions.CanFinalizeMatches, command.Request.Justificativa,
            previousMatch, correlationId, now, cancellationToken);
        if (previousState != SerieEstado.Anulada && series.Estado == SerieEstado.Anulada)
        {
            await SeriesCommandHandlerSupport.StageSeriesAuditAsync(
                auditRepository, series, AcaoAuditoriaCompetitiva.SerieAnulada,
                actorId, AuthPermissions.CanFinalizeMatches, command.Request.Justificativa,
                previousSeries, correlationId, now, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }
}

public sealed class CorrectMatchCommandHandler(
    ISerieRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    TimeProvider timeProvider,
    IValidator<CorrectMatchCommand> validator) : IRequestHandler<CorrectMatchCommand, Serie>
{
    public async Task<Serie> Handle(CorrectMatchCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeriesCommandHandlerSupport.GetActorId(actor);
        var series = await MatchCommandHandlerSupport.LoadSeriesByMatchAsync(repository, command.MatchId, cancellationToken);
        SeriesCommandHandlerSupport.EnsureExpectedVersion(series, command.ExpectedVersion);
        await MatchCommandHandlerSupport.AuthorizeTechnicalAsync(
            authorization, series, SeriesCommandOperations.TechnicalCorrection, cancellationToken);
        var match = MatchCommandHandlerSupport.GetMatch(series, command.MatchId);
        var previousMatch = SeriesCommandHandlerSupport.MatchSnapshot(match);
        var previousSeries = SeriesCommandHandlerSupport.SeriesSnapshot(series);
        var previousState = series.Estado;
        var now = timeProvider.GetUtcNow();
        ApplyCorrection(series, command, actorId, now);
        var correlationId = Guid.NewGuid();
        await MatchCommandHandlerSupport.StageMatchAuditAsync(
            auditRepository, match, AcaoAuditoriaCompetitiva.PartidaCorrigida,
            actorId, AuthPermissions.CanFinalizeMatches, command.Request.Justificativa,
            previousMatch, correlationId, now, cancellationToken);
        if (previousState != SerieEstado.Anulada && series.Estado == SerieEstado.Anulada)
        {
            await SeriesCommandHandlerSupport.StageSeriesAuditAsync(
                auditRepository, series, AcaoAuditoriaCompetitiva.SerieAnulada,
                actorId, AuthPermissions.CanFinalizeMatches, command.Request.Justificativa,
                previousSeries, correlationId, now, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return series;
    }

    private static void ApplyCorrection(
        Serie series,
        CorrectMatchCommand command,
        Guid actorId,
        DateTimeOffset now)
    {
        if (command.Request.Lados.HasValue && command.Request.Lados.Value is { } sides)
        {
            series.CorrigirPicks(command.MatchId, MatchCommandHandlerSupport.MapPicks(sides), actorId, now);
        }

        if (command.Request.LadoVencedorId.Value is Guid winnerId
            && command.Request.MotivoTermino.Value is MotivoTerminoPartida terminationReason)
        {
            series.CorrigirResultadoPartida(
                command.MatchId,
                winnerId,
                terminationReason,
                command.Request.AnularSerieSeInconclusiva.Value ?? false,
                actorId,
                now);
        }
    }
}

internal static class MatchCommandHandlerSupport
{
    public static async Task<Serie> LoadSeriesAsync(
        ISerieRepository repository,
        Guid seriesId,
        CancellationToken cancellationToken) =>
        await repository.GetAggregateAsync(seriesId, cancellationToken)
            ?? throw new DomainException(MessageCodes.CompetitiveSeriesNotFound);

    public static async Task<Serie> LoadSeriesByMatchAsync(
        ISerieRepository repository,
        Guid matchId,
        CancellationToken cancellationToken) =>
        await repository.GetAggregateByPartidaIdAsync(matchId, cancellationToken)
            ?? throw new DomainException(MessageCodes.CompetitiveMatchNotFound);

    public static Partida GetMatch(Serie series, Guid matchId) =>
        series.Partidas.SingleOrDefault(match => match.Id == matchId)
            ?? throw new DomainException(MessageCodes.CompetitiveMatchNotFound);

    public static IReadOnlyCollection<(Guid LadoSerieId, int ChampionId, int Ordem)> MapPicks(
        IReadOnlyCollection<MatchSidePicksRequestDto> sides) =>
        sides.SelectMany(side => side.ChampionIds.Select((championId, index) =>
            (side.LadoSerieId, championId, index + 1))).ToArray();

    public static Task AuthorizeTechnicalAsync(
        ICompetitiveAuthorizationService authorization,
        Serie series,
        string operation,
        CancellationToken cancellationToken) =>
        SeriesCommandHandlerSupport.AuthorizeAsync(
            authorization,
            AuthPermissions.CanFinalizeMatches,
            operation,
            nameof(Partida),
            series.Tipo,
            seasonState: null,
            hasStartedSeries: true,
            hasRequiredJustification: true,
            cancellationToken);

    public static Task StageMatchAuditAsync(
        ICompetitiveAuditRepository auditRepository,
        Partida match,
        AcaoAuditoriaCompetitiva action,
        Guid actorId,
        string capability,
        string? justification,
        SnapshotAuditoriaRedigido previous,
        Guid correlationId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        SeriesCommandHandlerSupport.AddAuditAsync(
            auditRepository, RecursoCompetitivoTipo.Partida, match.Id, action, actorId,
            capability, justification, previous, SeriesCommandHandlerSupport.MatchSnapshot(match),
            correlationId, occurredAt, cancellationToken);
}
