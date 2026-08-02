using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Seasons;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Rules;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Application.Handlers.Seasons;

public sealed class CreateSeasonCommandHandler(
    ICalendarioCompetitivoRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<CreateSeasonCommand> validator) : IRequestHandler<CreateSeasonCommand, Season>
{
    public async Task<Season> Handle(CreateSeasonCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeasonCommandHandlerSupport.GetActorId(actor);
        await SeasonCommandHandlerSupport.AuthorizeAsync(
            authorization,
            SeasonCommandOperations.Create,
            seasonState: null,
            cancellationToken);

        var request = command.Request;
        await SeasonCommandHandlerSupport.EnsureUniquePeriodAndOrderAsync(
            repository,
            request.Ano,
            request.OrdemNoAno,
            request.DataInicio,
            request.DataFimExclusiva,
            excludedSeasonId: null,
            cancellationToken);

        var now = clock.UtcNow;
        await repository.AcquireBootstrapLockAsync(cancellationToken);
        if (await repository.GetCalendarAsync(cancellationToken) is null)
        {
            await repository.AddAsync(
                new CalendarioCompetitivo(Guid.NewGuid(), actorId, now),
                cancellationToken);
        }

        var season = new Season(
            request.Nome,
            request.Ano,
            request.OrdemNoAno,
            request.DataInicio,
            request.DataFimExclusiva,
            actorId,
            now);
        await SeasonCommandHandlerSupport.ValidateChronologyAsync(
            repository,
            season,
            excludedSeasonId: null,
            cancellationToken);

        await repository.AddSeasonAsync(season, cancellationToken);
        await SeasonCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            season.Id,
            AcaoAuditoriaCompetitiva.TemporadaCriada,
            actorId,
            valorAnterior: null,
            SeasonCommandHandlerSupport.SeasonSnapshot(season),
            Guid.NewGuid(),
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return season;
    }
}

public sealed class UpdateSeasonCommandHandler(
    ICalendarioCompetitivoRepository repository,
    ISerieRepository serieRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<UpdateSeasonCommand> validator) : IRequestHandler<UpdateSeasonCommand, Season>
{
    public async Task<Season> Handle(UpdateSeasonCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeasonCommandHandlerSupport.GetActorId(actor);
        var season = await repository.GetSeasonByIdAsync(command.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        await SeasonCommandHandlerSupport.AuthorizeAsync(
            authorization,
            SeasonCommandOperations.Update,
            season.Estado,
            cancellationToken);
        SeasonCommandHandlerSupport.EnsureExpectedVersion(season.Versao, command.ExpectedVersion);

        var request = command.Request;
        var nome = request.Nome.HasValue ? request.Nome.Value! : season.Nome;
        var ano = request.Ano.HasValue ? request.Ano.Value!.Value : season.Ano;
        var ordemNoAno = request.OrdemNoAno.HasValue ? request.OrdemNoAno.Value!.Value : season.OrdemNoAno;
        var dataInicio = request.DataInicio.HasValue ? request.DataInicio.Value!.Value : season.DataInicio;
        var dataFimExclusiva = request.DataFimExclusiva.HasValue
            ? request.DataFimExclusiva.Value!.Value
            : season.DataFimExclusiva;

        await SeasonCommandHandlerSupport.EnsureUniquePeriodAndOrderAsync(
            repository,
            ano,
            ordemNoAno,
            dataInicio,
            dataFimExclusiva,
            season.Id,
            cancellationToken);
        if (await serieRepository.HasConfirmedSeriesOutsidePeriodAsync(
                season.Id,
                dataInicio,
                dataFimExclusiva,
                cancellationToken))
        {
            throw new DomainException(MessageCodes.SeasonPeriodInvalid);
        }

        var now = clock.UtcNow;
        var candidate = new Season(
            nome,
            ano,
            ordemNoAno,
            dataInicio,
            dataFimExclusiva,
            actorId,
            now);
        await SeasonCommandHandlerSupport.ValidateChronologyAsync(
            repository,
            candidate,
            season.Id,
            cancellationToken);

        var previous = SeasonCommandHandlerSupport.SeasonSnapshot(season);
        season.Atualizar(nome, ano, ordemNoAno, dataInicio, dataFimExclusiva, actorId, now);
        await SeasonCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            season.Id,
            AcaoAuditoriaCompetitiva.TemporadaAtualizada,
            actorId,
            previous,
            SeasonCommandHandlerSupport.SeasonSnapshot(season),
            Guid.NewGuid(),
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return season;
    }
}

public sealed class AtivarSeasonCommandHandler(
    ICalendarioCompetitivoRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<AtivarSeasonCommand> validator) : IRequestHandler<AtivarSeasonCommand, Season>
{
    public async Task<Season> Handle(AtivarSeasonCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeasonCommandHandlerSupport.GetActorId(actor);
        var calendar = await repository.GetWithSeasonsAsync(cancellationToken)
            ?? throw new DomainException(MessageCodes.ValidationError);
        var season = await repository.GetSeasonByIdAsync(command.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        await SeasonCommandHandlerSupport.AuthorizeAsync(
            authorization,
            SeasonCommandOperations.Activate,
            season.Estado,
            cancellationToken);
        var previousSeason = calendar.SeasonAtivaId is Guid previousId
            ? await repository.GetSeasonByIdAsync(previousId, cancellationToken)
                ?? throw new DomainException(MessageCodes.ValidationError)
            : null;
        var calendarBefore = SeasonCommandHandlerSupport.CalendarSnapshot(calendar);
        var seasonBefore = SeasonCommandHandlerSupport.SeasonSnapshot(season);
        var previousSeasonBefore = previousSeason is null
            ? null
            : SeasonCommandHandlerSupport.SeasonSnapshot(previousSeason);
        var now = clock.UtcNow;
        var correlationId = Guid.NewGuid();

        calendar.AtivarSeason(season, previousSeason, command.ExpectedVersion, actorId, now);
        await SeasonCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            calendar.Id,
            AcaoAuditoriaCompetitiva.CalendarioCompetitivoAtualizado,
            actorId,
            calendarBefore,
            SeasonCommandHandlerSupport.CalendarSnapshot(calendar),
            correlationId,
            now,
            cancellationToken,
            RecursoCompetitivoTipo.CalendarioCompetitivo);
        if (previousSeason is not null)
        {
            await SeasonCommandHandlerSupport.AddAuditAsync(
                auditRepository,
                previousSeason.Id,
                AcaoAuditoriaCompetitiva.TemporadaEncerrada,
                actorId,
                previousSeasonBefore,
                SeasonCommandHandlerSupport.SeasonSnapshot(previousSeason),
                correlationId,
                now,
                cancellationToken);
        }

        await SeasonCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            season.Id,
            AcaoAuditoriaCompetitiva.TemporadaAtivada,
            actorId,
            seasonBefore,
            SeasonCommandHandlerSupport.SeasonSnapshot(season),
            correlationId,
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return season;
    }
}

public sealed class EncerrarSeasonCommandHandler(
    ICalendarioCompetitivoRepository repository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<EncerrarSeasonCommand> validator) : IRequestHandler<EncerrarSeasonCommand, Season>
{
    public async Task<Season> Handle(EncerrarSeasonCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = SeasonCommandHandlerSupport.GetActorId(actor);
        var calendar = await repository.GetWithSeasonsAsync(cancellationToken)
            ?? throw new DomainException(MessageCodes.ValidationError);
        var season = await repository.GetSeasonByIdAsync(command.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        await SeasonCommandHandlerSupport.AuthorizeAsync(
            authorization,
            SeasonCommandOperations.Close,
            season.Estado,
            cancellationToken);
        var calendarBefore = SeasonCommandHandlerSupport.CalendarSnapshot(calendar);
        var seasonBefore = SeasonCommandHandlerSupport.SeasonSnapshot(season);
        var now = clock.UtcNow;
        var correlationId = Guid.NewGuid();

        calendar.EncerrarSeason(season, command.ExpectedVersion, actorId, now);
        await SeasonCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            calendar.Id,
            AcaoAuditoriaCompetitiva.CalendarioCompetitivoAtualizado,
            actorId,
            calendarBefore,
            SeasonCommandHandlerSupport.CalendarSnapshot(calendar),
            correlationId,
            now,
            cancellationToken,
            RecursoCompetitivoTipo.CalendarioCompetitivo);
        await SeasonCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            season.Id,
            AcaoAuditoriaCompetitiva.TemporadaEncerrada,
            actorId,
            seasonBefore,
            SeasonCommandHandlerSupport.SeasonSnapshot(season),
            correlationId,
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return season;
    }
}

internal static class SeasonCommandOperations
{
    public const string Create = "CriarSeason";
    public const string Update = "AtualizarSeason";
    public const string Activate = "AtivarSeason";
    public const string Close = "EncerrarSeason";
}

internal static class SeasonCommandHandlerSupport
{
    private const int SeasonPageSize = 100;

    public static Guid GetActorId(ICurrentActor actor) =>
        actor.UserId is Guid actorId && actorId != Guid.Empty
            ? actorId
            : throw new DomainException(MessageCodes.UnauthorizedAccess);

    public static async Task AuthorizeAsync(
        ICompetitiveAuthorizationService authorization,
        string operation,
        SeasonEstado? seasonState,
        CancellationToken cancellationToken)
    {
        var context = new CompetitiveAuthorizationContext(
            AuthPermissions.CanManageSeasons,
            operation,
            nameof(Season),
            SerieType: null,
            SeasonState: seasonState?.ToString(),
            HasStartedSeries: false,
            HasRequiredJustification: false);
        if (!await authorization.AuthorizeAsync(context, cancellationToken))
        {
            throw new DomainException(MessageCodes.CompetitiveAccessDenied);
        }
    }

    public static void EnsureExpectedVersion(long currentVersion, long expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new DomainException(MessageCodes.CompetitiveResourceVersionStale);
        }
    }

    public static async Task EnsureUniquePeriodAndOrderAsync(
        ICalendarioCompetitivoRepository repository,
        int ano,
        int ordemNoAno,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva,
        Guid? excludedSeasonId,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsOverlappingSeasonAsync(
                dataInicio, dataFimExclusiva, excludedSeasonId, cancellationToken))
        {
            throw new DomainException(MessageCodes.SeasonPeriodOverlap);
        }

        if (await repository.ExistsSeasonOrderAsync(
                ano, ordemNoAno, excludedSeasonId, cancellationToken))
        {
            throw new DomainException(MessageCodes.SeasonOrderConflict);
        }
    }

    public static async Task ValidateChronologyAsync(
        ICalendarioCompetitivoRepository repository,
        Season candidate,
        Guid? excludedSeasonId,
        CancellationToken cancellationToken)
    {
        var existing = new List<Season>();
        for (var page = 1; ; page++)
        {
            var items = await repository.ListSeasonsAsync(
                seasonIds: null,
                estado: null,
                page,
                SeasonPageSize,
                cancellationToken);
            existing.AddRange(items.Where(item => item.Id != excludedSeasonId));
            if (items.Count < SeasonPageSize)
            {
                break;
            }
        }

        SeasonRules.ValidarInclusao(candidate, existing);
    }

    public static SnapshotAuditoriaRedigido SeasonSnapshot(Season season) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = season.Id,
            [CampoSnapshotAuditoria.Nome] = season.Nome,
            [CampoSnapshotAuditoria.Ano] = season.Ano,
            [CampoSnapshotAuditoria.EstadoSeason] = season.Estado,
            [CampoSnapshotAuditoria.Versao] = season.Versao,
            [CampoSnapshotAuditoria.Ordem] = season.OrdemNoAno,
            [CampoSnapshotAuditoria.DataInicio] = season.DataInicio,
            [CampoSnapshotAuditoria.DataFimExclusiva] = season.DataFimExclusiva
        });

    public static SnapshotAuditoriaRedigido CalendarSnapshot(CalendarioCompetitivo calendar) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = calendar.Id,
            [CampoSnapshotAuditoria.SeasonId] = calendar.SeasonAtivaId,
            [CampoSnapshotAuditoria.Versao] = calendar.Versao
        });

    public static Task AddAuditAsync(
        ICompetitiveAuditRepository auditRepository,
        Guid resourceId,
        AcaoAuditoriaCompetitiva action,
        Guid actorId,
        SnapshotAuditoriaRedigido? valorAnterior,
        SnapshotAuditoriaRedigido? valorPosterior,
        Guid correlationId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken,
        RecursoCompetitivoTipo resourceType = RecursoCompetitivoTipo.Season) =>
        auditRepository.AddAsync(
            new RegistroAuditoriaCompetitiva(
                resourceType,
                resourceId,
                action,
                actorId,
                AuthPermissions.CanManageSeasons,
                justificativa: null,
                valorAnterior,
                valorPosterior,
                correlationId,
                occurredAt),
            cancellationToken);
}
