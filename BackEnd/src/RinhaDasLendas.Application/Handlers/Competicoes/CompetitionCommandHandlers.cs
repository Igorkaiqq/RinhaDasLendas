using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Competicoes;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Application.Handlers.Competicoes;

public sealed class CreateCompetitionCommandHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<CreateCompetitionCommand> validator) : IRequestHandler<CreateCompetitionCommand, Competicao>
{
    public async Task<Competicao> Handle(CreateCompetitionCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = CompetitionCommandHandlerSupport.GetActorId(actor);
        var season = await calendarRepository.GetSeasonByIdAsync(command.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        await CompetitionCommandHandlerSupport.AuthorizeConfigurationAsync(
            authorization,
            seriesRepository,
            season,
            competitionId: null,
            CompetitionCommandOperations.ConfigureCompetition,
            nameof(Competicao),
            cancellationToken);

        var request = command.Request;
        await CompetitionCommandHandlerSupport.EnsureUniqueCompetitionAsync(
            competitionRepository,
            season.Id,
            request.Codigo,
            request.CircuitoDiario.Value!.Value,
            excludedCompetitionId: null,
            cancellationToken);

        var now = clock.UtcNow;
        var competition = new Competicao(
            season.Id,
            request.Nome,
            request.Codigo,
            request.CircuitoDiario.Value!.Value,
            actorId,
            now);
        await competitionRepository.AddAsync(competition, cancellationToken);
        await CompetitionCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            RecursoCompetitivoTipo.Competicao,
            competition.Id,
            AcaoAuditoriaCompetitiva.CompeticaoCriada,
            actorId,
            valorAnterior: null,
            CompetitionCommandHandlerSupport.CompetitionSnapshot(competition),
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return competition;
    }
}

public sealed class UpdateCompetitionCommandHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<UpdateCompetitionCommand> validator) : IRequestHandler<UpdateCompetitionCommand, Competicao>
{
    public async Task<Competicao> Handle(UpdateCompetitionCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = CompetitionCommandHandlerSupport.GetActorId(actor);
        var competition = await competitionRepository.GetWithRoundsAndRulesAsync(
                command.CompetitionId,
                cancellationToken)
            ?? throw new DomainException(MessageCodes.CompetitionNotFound);
        var season = await calendarRepository.GetSeasonByIdAsync(competition.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        await CompetitionCommandHandlerSupport.AuthorizeConfigurationAsync(
            authorization,
            seriesRepository,
            season,
            competition.Id,
            CompetitionCommandOperations.ConfigureCompetition,
            nameof(Competicao),
            cancellationToken);
        CompetitionCommandHandlerSupport.EnsureExpectedVersion(competition.Versao, command.ExpectedVersion);

        var request = command.Request;
        var nome = request.Nome.HasValue ? request.Nome.Value! : competition.Nome;
        var codigo = request.Codigo.HasValue ? request.Codigo.Value! : competition.Codigo;
        var circuitoDiario = request.CircuitoDiario.HasValue
            ? request.CircuitoDiario.Value!.Value
            : competition.CircuitoDiario;
        await CompetitionCommandHandlerSupport.EnsureUniqueCompetitionAsync(
            competitionRepository,
            competition.SeasonId,
            codigo,
            circuitoDiario,
            competition.Id,
            cancellationToken);

        var before = CompetitionCommandHandlerSupport.CompetitionSnapshot(competition);
        var now = clock.UtcNow;
        competition.Atualizar(nome, codigo, circuitoDiario, command.ExpectedVersion, actorId, now);
        await CompetitionCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            RecursoCompetitivoTipo.Competicao,
            competition.Id,
            AcaoAuditoriaCompetitiva.CompeticaoAtualizada,
            actorId,
            before,
            CompetitionCommandHandlerSupport.CompetitionSnapshot(competition),
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return competition;
    }
}

public sealed class CreateRoundCommandHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<CreateRoundCommand> validator) : IRequestHandler<CreateRoundCommand, Rodada>
{
    public async Task<Rodada> Handle(CreateRoundCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = CompetitionCommandHandlerSupport.GetActorId(actor);
        var (competition, season) = await CompetitionCommandHandlerSupport.LoadCompetitionScopeAsync(
            calendarRepository,
            competitionRepository,
            command.CompetitionId,
            cancellationToken);
        await CompetitionCommandHandlerSupport.AuthorizeConfigurationAsync(
            authorization,
            seriesRepository,
            season,
            competition.Id,
            CompetitionCommandOperations.ConfigureRound,
            nameof(Rodada),
            cancellationToken);
        CompetitionCommandHandlerSupport.EnsureExpectedVersion(competition.Versao, command.ExpectedVersion);

        var now = clock.UtcNow;
        var round = competition.AdicionarRodada(command.Request.Nome, command.Request.Ordem, now, actorId);
        await competitionRepository.AddRoundAsync(round, cancellationToken);
        await CompetitionCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            RecursoCompetitivoTipo.Rodada,
            round.Id,
            AcaoAuditoriaCompetitiva.RodadaCriada,
            actorId,
            valorAnterior: null,
            CompetitionCommandHandlerSupport.RoundSnapshot(round),
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return round;
    }
}

public sealed class ReorderRoundsCommandHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<ReorderRoundsCommand> validator) : IRequestHandler<ReorderRoundsCommand, IReadOnlyCollection<Rodada>>
{
    public async Task<IReadOnlyCollection<Rodada>> Handle(
        ReorderRoundsCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = CompetitionCommandHandlerSupport.GetActorId(actor);
        var (competition, season) = await CompetitionCommandHandlerSupport.LoadCompetitionScopeAsync(
            calendarRepository,
            competitionRepository,
            command.CompetitionId,
            cancellationToken);
        await CompetitionCommandHandlerSupport.AuthorizeConfigurationAsync(
            authorization,
            seriesRepository,
            season,
            competition.Id,
            CompetitionCommandOperations.ConfigureRound,
            nameof(Rodada),
            cancellationToken);
        CompetitionCommandHandlerSupport.EnsureExpectedVersion(competition.Versao, command.ExpectedVersion);

        var before = CompetitionCommandHandlerSupport.RoundOrderSnapshot(competition);
        var now = clock.UtcNow;
        competition.ReordenarRodadas(command.Request.RodadaIds, command.ExpectedVersion, actorId, now);
        await CompetitionCommandHandlerSupport.AddAuditAsync(
            auditRepository,
            RecursoCompetitivoTipo.Competicao,
            competition.Id,
            AcaoAuditoriaCompetitiva.RodadasReordenadas,
            actorId,
            before,
            CompetitionCommandHandlerSupport.RoundOrderSnapshot(competition),
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return competition.Rodadas;
    }
}

public sealed class PublishSeasonRulesCommandHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<PublishSeasonRulesCommand> validator) : IRequestHandler<PublishSeasonRulesCommand, VersaoRegras>
{
    public async Task<VersaoRegras> Handle(
        PublishSeasonRulesCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = CompetitionCommandHandlerSupport.GetActorId(actor);
        var season = await calendarRepository.GetSeasonByIdAsync(command.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        await CompetitionCommandHandlerSupport.AuthorizeRulesPublicationAsync(
            authorization,
            season.Estado,
            cancellationToken);
        CompetitionCommandHandlerSupport.EnsureExpectedVersion(season.Versao, command.ExpectedVersion);

        var nextNumber = await competitionRepository.GetNextGeneralRulesNumberAsync(
            season.Id,
            cancellationToken);
        var now = clock.UtcNow;
        var rules = season.PublicarRegrasGerais(
             command.Request.Formato.Value!.Value,
             command.Request.ModoDraft.Value!.Value,
            command.ExpectedVersion,
            actorId,
            now,
            nextNumber);
        await competitionRepository.AddRulesVersionAsync(rules, cancellationToken);
        await CompetitionCommandHandlerSupport.AuditRulesAsync(
            auditRepository,
            rules,
            actorId,
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return rules;
    }
}

public sealed class PublishCompetitionRulesCommandHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ICompetitiveUnitOfWork unitOfWork,
    ICurrentActor actor,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveAuditRepository auditRepository,
    ISystemClock clock,
    IValidator<PublishCompetitionRulesCommand> validator) : IRequestHandler<PublishCompetitionRulesCommand, VersaoRegras>
{
    public async Task<VersaoRegras> Handle(
        PublishCompetitionRulesCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var actorId = CompetitionCommandHandlerSupport.GetActorId(actor);
        var (competition, season) = await CompetitionCommandHandlerSupport.LoadCompetitionScopeAsync(
            calendarRepository,
            competitionRepository,
            command.CompetitionId,
            cancellationToken);
        await CompetitionCommandHandlerSupport.AuthorizeRulesPublicationAsync(
            authorization,
            season.Estado,
            cancellationToken);
        CompetitionCommandHandlerSupport.EnsureExpectedVersion(competition.Versao, command.ExpectedVersion);

        var now = clock.UtcNow;
        var rules = competition.PublicarRegras(
             command.Request.Formato.Value!.Value,
             command.Request.ModoDraft.Value!.Value,
            command.ExpectedVersion,
            actorId,
            now);
        await competitionRepository.AddRulesVersionAsync(rules, cancellationToken);
        await CompetitionCommandHandlerSupport.AuditRulesAsync(
            auditRepository,
            rules,
            actorId,
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return rules;
    }
}

internal static class CompetitionCommandOperations
{
    public const string ConfigureCompetition = "ConfigurarCompeticao";
    public const string ConfigureRound = "ConfigurarRodada";
    public const string Publish = "Publicar";
}

internal static class CompetitionCommandHandlerSupport
{
    public static Guid GetActorId(ICurrentActor actor) =>
        actor.UserId is Guid actorId && actorId != Guid.Empty
            ? actorId
            : throw new DomainException(MessageCodes.UnauthorizedAccess);

    public static void EnsureExpectedVersion(long currentVersion, long expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new DomainException(MessageCodes.CompetitiveResourceVersionStale);
        }
    }

    public static async Task<(Competicao Competition, Season Season)> LoadCompetitionScopeAsync(
        ICalendarioCompetitivoRepository calendarRepository,
        ICompeticaoRepository competitionRepository,
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        var competition = await competitionRepository.GetWithRoundsAndRulesAsync(
                competitionId,
                cancellationToken)
            ?? throw new DomainException(MessageCodes.CompetitionNotFound);
        var season = await calendarRepository.GetSeasonByIdAsync(competition.SeasonId, cancellationToken)
            ?? throw new DomainException(MessageCodes.SeasonNotFound);
        return (competition, season);
    }

    public static async Task AuthorizeConfigurationAsync(
        ICompetitiveAuthorizationService authorization,
        ISerieRepository seriesRepository,
        Season season,
        Guid? competitionId,
        string operation,
        string resourceType,
        CancellationToken cancellationToken)
    {
        var hasStartedSeries = await seriesRepository.HasStartedSeriesAsync(
            season.Id,
            competitionId,
            cancellationToken);
        var context = new CompetitiveAuthorizationContext(
            AuthPermissions.CanManageCompetitions,
            operation,
            resourceType,
            SerieType: null,
            SeasonState: season.Estado.ToString(),
            HasStartedSeries: hasStartedSeries,
            HasRequiredJustification: false);
        if (!await authorization.AuthorizeAsync(context, cancellationToken))
        {
            throw new DomainException(MessageCodes.CompetitiveAccessDenied);
        }
    }

    public static async Task AuthorizeRulesPublicationAsync(
        ICompetitiveAuthorizationService authorization,
        SeasonEstado seasonState,
        CancellationToken cancellationToken)
    {
        var context = new CompetitiveAuthorizationContext(
            AuthPermissions.CanManageCompetitions,
            CompetitionCommandOperations.Publish,
            "Regra",
            SerieType: null,
            SeasonState: seasonState.ToString(),
            HasStartedSeries: false,
            HasRequiredJustification: false);
        if (!await authorization.AuthorizeAsync(context, cancellationToken))
        {
            throw new DomainException(MessageCodes.CompetitiveAccessDenied);
        }
    }

    public static async Task EnsureUniqueCompetitionAsync(
        ICompeticaoRepository repository,
        Guid seasonId,
        string code,
        bool dailyCircuit,
        Guid? excludedCompetitionId,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsCodeAsync(
                seasonId,
                code.Trim(),
                excludedCompetitionId,
                cancellationToken))
        {
            throw new DomainException(MessageCodes.CompetitionCodeConflict);
        }

        if (dailyCircuit && await repository.ExistsDailyCircuitAsync(
                seasonId,
                excludedCompetitionId,
                cancellationToken))
        {
            throw new DomainException(MessageCodes.DailyCircuitCompetitionInvalid);
        }
    }

    public static SnapshotAuditoriaRedigido CompetitionSnapshot(Competicao competition) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = competition.Id,
            [CampoSnapshotAuditoria.SeasonId] = competition.SeasonId,
            [CampoSnapshotAuditoria.Nome] = competition.Nome,
            [CampoSnapshotAuditoria.Codigo] = competition.Codigo,
            [CampoSnapshotAuditoria.CircuitoDiario] = competition.CircuitoDiario,
            [CampoSnapshotAuditoria.Versao] = competition.Versao
        });

    public static SnapshotAuditoriaRedigido RoundSnapshot(Rodada round) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = round.Id,
            [CampoSnapshotAuditoria.CompeticaoId] = round.CompeticaoId,
            [CampoSnapshotAuditoria.Nome] = round.Nome,
            [CampoSnapshotAuditoria.Ordem] = round.Ordem,
            [CampoSnapshotAuditoria.Versao] = round.Versao
        });

    public static SnapshotAuditoriaRedigido RoundOrderSnapshot(Competicao competition) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = competition.Id,
            [CampoSnapshotAuditoria.Versao] = competition.Versao,
            [CampoSnapshotAuditoria.RodadaIds] = competition.Rodadas.Select(round => round.Id).ToArray()
        });

    public static SnapshotAuditoriaRedigido RulesSnapshot(VersaoRegras rules) =>
        SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = rules.Id,
            [CampoSnapshotAuditoria.SeasonId] = rules.SeasonId,
            [CampoSnapshotAuditoria.CompeticaoId] = rules.CompeticaoId,
            [CampoSnapshotAuditoria.Numero] = rules.Numero,
            [CampoSnapshotAuditoria.FormatoSerie] = rules.Formato,
            [CampoSnapshotAuditoria.ModoDraft] = rules.ModoDraft
        });

    public static Task AuditRulesAsync(
        ICompetitiveAuditRepository auditRepository,
        VersaoRegras rules,
        Guid actorId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        AddAuditAsync(
            auditRepository,
            RecursoCompetitivoTipo.VersaoRegras,
            rules.Id,
            rules.CompeticaoId is null
                ? AcaoAuditoriaCompetitiva.RegrasGeraisSeasonPublicadas
                : AcaoAuditoriaCompetitiva.RegrasCompeticaoPublicadas,
            actorId,
            valorAnterior: null,
            RulesSnapshot(rules),
            occurredAt,
            cancellationToken);

    public static Task AddAuditAsync(
        ICompetitiveAuditRepository auditRepository,
        RecursoCompetitivoTipo resourceType,
        Guid resourceId,
        AcaoAuditoriaCompetitiva action,
        Guid actorId,
        SnapshotAuditoriaRedigido? valorAnterior,
        SnapshotAuditoriaRedigido? valorPosterior,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        auditRepository.AddAsync(
            new RegistroAuditoriaCompetitiva(
                resourceType,
                resourceId,
                action,
                actorId,
                AuthPermissions.CanManageCompetitions,
                justificativa: null,
                valorAnterior,
                valorPosterior,
                Guid.NewGuid(),
                occurredAt),
            cancellationToken);
}
