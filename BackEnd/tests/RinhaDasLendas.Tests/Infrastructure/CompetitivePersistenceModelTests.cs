using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.ValueObjects;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Infrastructure;

public sealed class CompetitivePersistenceModelTests
{
    private static readonly IReadOnlyDictionary<Type, string> ExpectedTables =
        new Dictionary<Type, string>
        {
            [typeof(CalendarioCompetitivo)] = "calendarios_competitivos",
            [typeof(Season)] = "seasons",
            [typeof(Competicao)] = "competicoes",
            [typeof(Rodada)] = "rodadas",
            [typeof(VersaoRegras)] = "versoes_regras",
            [typeof(EventoCompetitivo)] = "eventos_competitivos",
            [typeof(EventoTime)] = "evento_times",
            [typeof(Serie)] = "series",
            [typeof(LadoSerie)] = "lados_series",
            [typeof(ParticipanteEsperadoSerie)] = "participantes_esperados_series",
            [typeof(Partida)] = "partidas",
            [typeof(PickPartida)] = "picks_partidas",
            [typeof(RegistroAuditoriaCompetitiva)] = "registros_auditoria_competitiva",
            [typeof(OperacaoIdempotente)] = "operacoes_idempotentes",
        };

    [Fact]
    public void CompetitiveModel_ShouldMapAllEntitiesThroughExtractedConfigurations()
    {
        using var context = CreateContext();

        foreach (var (clrType, tableName) in ExpectedTables)
        {
            var entityType = context.Model.FindEntityType(clrType);
            entityType.Should().NotBeNull();
            entityType!.GetTableName().Should().Be(tableName);
            entityType.FindPrimaryKey()!.Properties.Should().ContainSingle(property =>
                property.Name == "Id"
                && property.ClrType == typeof(Guid)
                && property.ValueGenerated == ValueGenerated.Never);
        }

        var configurationTypes = typeof(RinhaDasLendasDbContext).Assembly.GetTypes()
            .Where(type => type.Namespace == "RinhaDasLendas.Infrastructure.Persistence.Configurations")
            .Where(type => type.GetInterfaces().Any(@interface =>
                @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
            .ToArray();
        configurationTypes.Should().HaveCount(ExpectedTables.Count);
        configurationTypes.Should().OnlyContain(type => type.IsSealed);
    }

    [Fact]
    public void CompetitiveModel_ShouldMatchExactConcurrencyIndexesAndRestrictiveRelationships()
    {
        using var context = CreateContext();
        var entityTypes = ExpectedTables.Keys.Select(type => context.Model.FindEntityType(type)!).ToArray();

        Type[] versionedTypes =
        [
            typeof(CalendarioCompetitivo),
            typeof(Season),
            typeof(Competicao),
            typeof(Rodada),
            typeof(EventoCompetitivo),
            typeof(Serie),
            typeof(Partida),
        ];
        versionedTypes.Should().OnlyContain(type =>
            context.Model.FindEntityType(type)!.FindProperty("Versao")!.IsConcurrencyToken);

        entityTypes.SelectMany(entityType => entityType.GetForeignKeys()).Should().HaveCount(36);
        entityTypes.SelectMany(entityType => entityType.GetForeignKeys())
            .Should().OnlyContain(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict
                || foreignKey.DeleteBehavior == DeleteBehavior.NoAction);
        entityTypes.SelectMany(entityType => entityType.GetForeignKeys()).Should().OnlyContain(foreignKey =>
            foreignKey.DeclaringEntityType.GetIndexes().Any(index =>
                index.Properties.Take(foreignKey.Properties.Count).SequenceEqual(foreignKey.Properties)));

        var indexes = entityTypes.SelectMany(entityType => entityType.GetIndexes())
            .Select(index => index.GetDatabaseName())
            .Order(StringComparer.Ordinal)
            .ToArray();
        indexes.Should().Equal(
            "ix_calendarios_competitivos_atualizado_por_usuario_id",
            "ix_calendarios_competitivos_season_ativa_id",
            "ix_competicoes_atualizada_por_usuario_id",
            "ix_competicoes_criada_por_usuario_id",
            "ix_evento_times_time_id",
            "ix_eventos_competitivos_atualizado_por_usuario_id",
            "ix_eventos_competitivos_criado_por_usuario_id",
            "ix_eventos_competitivos_season_id",
            "ix_lados_series_capitao_jogador_id",
            "ix_participantes_esperados_series_jogador_id",
            "ix_participantes_esperados_series_lado_serie_id",
            "ix_partidas_lado_vencedor_id_serie_id",
            "ix_picks_partidas_lado_serie_id_serie_id",
            "ix_picks_partidas_partida_id_serie_id",
            "ix_registros_auditoria_competitiva_ator_usuario_id",
            "ix_rodadas_competicao_id",
            "ix_seasons_atualizada_por_usuario_id",
            "ix_seasons_criada_por_usuario_id",
            "ix_series_atualizada_por_usuario_id",
            "ix_series_competicao_id_season_id",
            "ix_series_criada_por_usuario_id",
            "ix_series_draft_montagem_id",
            "ix_series_evento_id_season_id",
            "ix_series_lado_vencedor_id_id",
            "ix_series_rodada_id_competicao_id",
            "ix_series_season_id",
            "ix_series_versao_regras_id_competicao_id",
            "ix_series_versao_regras_id_season_id",
            "ix_versoes_regras_competicao_id_season_id",
            "ix_versoes_regras_publicada_por_usuario_id",
            "ix_versoes_regras_season_id",
            "ux_competicoes_circuito_diario",
            "ux_evento_times_evento_id_ordem",
            "ux_evento_times_evento_id_time_id",
            "ux_lados_series_serie_id_ordem",
            "ux_operacoes_idempotentes_ator_metodo_rota_chave",
            "ux_partidas_serie_id_ordem",
            "ux_picks_partidas_champion_valido",
            "ux_picks_partidas_slot_valido",
            "ux_seasons_ano_ordem_no_ano",
            "ux_seasons_ativa",
            "ux_versoes_regras_competicao_numero",
            "ux_versoes_regras_season_numero_geral");

        var pickType = context.Model.FindEntityType(typeof(PickPartida))!;
        pickType.FindProperty("SerieId")!.IsShadowProperty().Should().BeTrue();
        pickType.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual(new[] { "PartidaId", "SerieId" }));
        pickType.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual(new[] { "LadoSerieId", "SerieId" }));

        var validPickIndexes = pickType.GetIndexes().Where(index => index.IsUnique).ToArray();
        validPickIndexes.Should().Contain(index =>
            index.GetDatabaseName() == "ux_picks_partidas_slot_valido" && index.GetFilter() == "valido");
        validPickIndexes.Should().Contain(index =>
            index.GetDatabaseName() == "ux_picks_partidas_champion_valido" && index.GetFilter() == "valido");

        var matchConstraints = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Partida))!.GetCheckConstraints();
        matchConstraints.Should().Contain(constraint =>
            constraint.Name == "ck_partidas_estado_resultado_coerente"
            && constraint.Sql == "(estado = 'Confirmada' AND lado_vencedor_id IS NOT NULL AND motivo_termino IS NOT NULL AND confirmada_em IS NOT NULL) OR (estado <> 'Confirmada' AND lado_vencedor_id IS NULL AND motivo_termino IS NULL AND confirmada_em IS NULL)");
        matchConstraints.Should().Contain(constraint =>
            constraint.Name == "ck_partidas_estado_remake_coerente"
            && constraint.Sql == "(estado = 'Remake' AND decisao_picks_remake IS NOT NULL) OR (estado <> 'Remake' AND decisao_picks_remake IS NULL)");

        typeof(RinhaDasLendasDbContext).Assembly
            .GetType("RinhaDasLendas.Infrastructure.Persistence.Configurations.CompetitiveForeignKeyIndexConvention")
            .Should().BeNull();

        context.Model.FindEntityType(typeof(CalendarioCompetitivo))!
            .FindAnnotation("RinhaDasLendas:ExpressionIndex:ux_calendarios_competitivos_singleton")!
            .Value.Should().Be("(true)");
        context.Model.FindEntityType(typeof(Competicao))!
            .FindAnnotation("RinhaDasLendas:ExpressionIndex:ux_competicoes_season_id_codigo")!
            .Value.Should().Be("(season_id, lower(codigo))");
        context.Model.FindEntityType(typeof(Rodada))!
            .FindAnnotation("RinhaDasLendas:DeferrableUniqueConstraint:ux_rodadas_competicao_id_ordem")!
            .Value.Should().Be("UNIQUE (competicao_id, ordem) DEFERRABLE INITIALLY DEFERRED");
        context.Model.FindEntityType(typeof(Season))!
            .FindAnnotation("Npgsql:ExclusionConstraint:ex_seasons_periodo")!
            .Value.Should().Be("daterange(data_inicio, data_fim_exclusiva, '[)') WITH &&");
    }

    [Fact]
    public void CompetitiveModel_ShouldRepresentEveryCompetitionOwnershipRelationship()
    {
        using var context = CreateContext();
        var competitionType = context.Model.FindEntityType(typeof(Competicao))!;
        var rulesType = context.Model.FindEntityType(typeof(VersaoRegras))!;
        var seriesType = context.Model.FindEntityType(typeof(Serie))!;

        rulesType.GetForeignKeys().Should().ContainSingle(foreignKey =>
            foreignKey.PrincipalEntityType == competitionType
            && foreignKey.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "CompeticaoId", "SeasonId" }));
        rulesType.FindProperty("CompeticaoEscopoId").Should().NotBeNull();
        AssertStoredComputedShadowScope(rulesType);
        typeof(VersaoRegras).GetProperty("CompeticaoEscopoId", BindingFlags.Instance | BindingFlags.NonPublic)
            .Should().BeNull();
        rulesType.GetKeys().Should().ContainSingle(key =>
            key.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "Id", "CompeticaoEscopoId" }));
        seriesType.FindProperty("CompeticaoEscopoId").Should().NotBeNull();
        AssertStoredComputedShadowScope(seriesType);
        typeof(Serie).GetProperty("CompeticaoEscopoId", BindingFlags.Instance | BindingFlags.NonPublic)
            .Should().BeNull();
        seriesType.GetForeignKeys().Should().ContainSingle(foreignKey =>
            foreignKey.PrincipalEntityType == rulesType
            && foreignKey.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "VersaoRegrasId", "CompeticaoEscopoId" })
            && foreignKey.PrincipalKey.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "Id", "CompeticaoEscopoId" }));
    }

    private static void AssertStoredComputedShadowScope(IEntityType entityType)
    {
        var property = entityType.FindProperty("CompeticaoEscopoId")!;

        property.IsShadowProperty().Should().BeTrue();
        property.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        property.GetComputedColumnSql().Should().Be(
            "COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)");
        property.GetIsStored().Should().BeTrue();
        property.GetBeforeSaveBehavior().Should().Be(PropertySaveBehavior.Ignore);
        property.GetAfterSaveBehavior().Should().Be(PropertySaveBehavior.Ignore);
    }

    [Fact]
    public void AuditSnapshotConverter_ShouldPreserveNullThroughProviderAndMaterializationExpressions()
    {
        using var context = CreateContext();
        var converter = GetAuditSnapshotConverter(context);

        var providerValue = converter.ConvertToProviderExpression.Compile().DynamicInvoke([null]);
        var materializedValue = converter.ConvertFromProviderExpression.Compile().DynamicInvoke([null]);

        providerValue.Should().BeNull();
        materializedValue.Should().BeNull();
    }

    [Fact]
    public void AuditSnapshotConverter_ShouldRoundTripEverySupportedFieldCanonicallyInUtc()
    {
        using var context = CreateContext();
        var converter = GetAuditSnapshotConverter(context);
        var snapshot = SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            [CampoSnapshotAuditoria.SeasonId] = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            [CampoSnapshotAuditoria.CompeticaoId] = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            [CampoSnapshotAuditoria.RodadaId] = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            [CampoSnapshotAuditoria.VersaoRegrasId] = Guid.Parse("00000000-0000-0000-0000-000000000005"),
            [CampoSnapshotAuditoria.EventoId] = Guid.Parse("00000000-0000-0000-0000-000000000006"),
            [CampoSnapshotAuditoria.SerieId] = Guid.Parse("00000000-0000-0000-0000-000000000007"),
            [CampoSnapshotAuditoria.PartidaId] = Guid.Parse("00000000-0000-0000-0000-000000000008"),
            [CampoSnapshotAuditoria.LadoSerieId] = Guid.Parse("00000000-0000-0000-0000-000000000009"),
            [CampoSnapshotAuditoria.TimeId] = Guid.Parse("00000000-0000-0000-0000-00000000000a"),
            [CampoSnapshotAuditoria.DraftMontagemId] = Guid.Parse("00000000-0000-0000-0000-00000000000b"),
            [CampoSnapshotAuditoria.EstadoSeason] = SeasonEstado.Ativa,
            [CampoSnapshotAuditoria.EstadoSerie] = SerieEstado.EmAndamento,
            [CampoSnapshotAuditoria.EstadoPartida] = PartidaEstado.Confirmada,
            [CampoSnapshotAuditoria.Versao] = 7L,
            [CampoSnapshotAuditoria.TipoSerie] = SerieTipo.Amistoso,
            [CampoSnapshotAuditoria.TipoLado] = LadoSerieTipo.TimeOficial,
            [CampoSnapshotAuditoria.FormatoSerie] = SerieFormato.Md5,
            [CampoSnapshotAuditoria.ModoDraft] = ModoDraft.Fearless,
            [CampoSnapshotAuditoria.Ordem] = 2,
            [CampoSnapshotAuditoria.Resultado] = Array.AsReadOnly(new[] { 2, 1 }),
            [CampoSnapshotAuditoria.Picks] = Array.AsReadOnly(new[] { 11, 22 }),
            [CampoSnapshotAuditoria.DataInicio] = new DateOnly(2026, 7, 1),
            [CampoSnapshotAuditoria.DataFimExclusiva] = new DateOnly(2026, 8, 1),
            [CampoSnapshotAuditoria.AgendadaPara] = new DateTimeOffset(2026, 7, 29, 12, 34, 56, TimeSpan.FromHours(-3)),
            [CampoSnapshotAuditoria.DataLocal] = new DateOnly(2026, 7, 29),
            [CampoSnapshotAuditoria.LadoVencedorId] = Guid.Parse("00000000-0000-0000-0000-00000000000c"),
            [CampoSnapshotAuditoria.FearlessHabilitado] = true,
            [CampoSnapshotAuditoria.RevisaoNecessaria] = false,
            [CampoSnapshotAuditoria.DecisaoPicksRemake] = DecisaoPicksRemake.PreservarPicks,
            [CampoSnapshotAuditoria.MotivoTerminoPartida] = MotivoTerminoPartida.Surrender,
            [CampoSnapshotAuditoria.Nome] = "Season 2026",
            [CampoSnapshotAuditoria.Ano] = 2026,
        });
        const string expected = "{\"Id\":\"00000000-0000-0000-0000-000000000001\",\"SeasonId\":\"00000000-0000-0000-0000-000000000002\",\"CompeticaoId\":\"00000000-0000-0000-0000-000000000003\",\"RodadaId\":\"00000000-0000-0000-0000-000000000004\",\"VersaoRegrasId\":\"00000000-0000-0000-0000-000000000005\",\"EventoId\":\"00000000-0000-0000-0000-000000000006\",\"SerieId\":\"00000000-0000-0000-0000-000000000007\",\"PartidaId\":\"00000000-0000-0000-0000-000000000008\",\"LadoSerieId\":\"00000000-0000-0000-0000-000000000009\",\"TimeId\":\"00000000-0000-0000-0000-00000000000a\",\"DraftMontagemId\":\"00000000-0000-0000-0000-00000000000b\",\"EstadoSeason\":\"Ativa\",\"EstadoSerie\":\"EmAndamento\",\"EstadoPartida\":\"Confirmada\",\"Versao\":7,\"TipoSerie\":\"Amistoso\",\"TipoLado\":\"TimeOficial\",\"FormatoSerie\":\"Md5\",\"ModoDraft\":\"Fearless\",\"Ordem\":2,\"Resultado\":[2,1],\"Picks\":[11,22],\"DataInicio\":\"2026-07-01\",\"DataFimExclusiva\":\"2026-08-01\",\"AgendadaPara\":\"2026-07-29T15:34:56+00:00\",\"DataLocal\":\"2026-07-29\",\"LadoVencedorId\":\"00000000-0000-0000-0000-00000000000c\",\"FearlessHabilitado\":true,\"RevisaoNecessaria\":false,\"DecisaoPicksRemake\":\"PreservarPicks\",\"MotivoTerminoPartida\":\"Surrender\",\"Nome\":\"Season 2026\",\"Ano\":2026}";

        var providerValue = converter.ConvertToProviderExpression.Compile().DynamicInvoke([snapshot]);
        var materialized = converter.ConvertFromProviderExpression.Compile().DynamicInvoke([providerValue])
            .Should().BeOfType<SnapshotAuditoriaRedigido>().Subject;

        providerValue.Should().Be(expected);
        materialized.ValorSerializado.Should().Be(expected);
        materialized.Campos[CampoSnapshotAuditoria.AgendadaPara]
            .Should().Be(new DateTimeOffset(2026, 7, 29, 15, 34, 56, TimeSpan.Zero));
    }

    [Fact]
    public async Task CompetitiveUnitOfWork_ShouldSaveTheSuppliedContextOnly()
    {
        var interceptor = new SuppressingSaveChangesInterceptor();
        await using var context = CreateContext(interceptor);
        var unitOfWorkType = typeof(RinhaDasLendasDbContext).Assembly
            .GetType("RinhaDasLendas.Infrastructure.Persistence.CompetitiveUnitOfWork");

        unitOfWorkType.Should().NotBeNull();
        unitOfWorkType!.Should().BeSealed();
        var constructor = unitOfWorkType.GetConstructors().Should().ContainSingle().Subject;
        constructor.GetParameters().Should().ContainSingle(parameter =>
            parameter.ParameterType == typeof(RinhaDasLendasDbContext));
        var unitOfWork = constructor.Invoke([context]).Should().BeAssignableTo<ICompetitiveUnitOfWork>().Subject;

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        interceptor.Context.Should().BeSameAs(context);
        interceptor.Calls.Should().Be(1);
    }

    [Fact]
    public async Task CompetitiveUnitOfWork_DeferredScope_ShouldNoOpInterfaceSaveAndFlushOnceExplicitly()
    {
        var interceptor = new SuppressingSaveChangesInterceptor();
        await using var context = CreateContext(interceptor);
        var unitOfWork = new CompetitiveUnitOfWork(context);
        var beginScope = typeof(CompetitiveUnitOfWork).GetMethod(
            "BeginDeferredSaveScope",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var flush = typeof(CompetitiveUnitOfWork).GetMethod(
            "FlushDeferredChangesAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var saveRequested = typeof(CompetitiveUnitOfWork).GetProperty(
            "DeferredSaveRequested",
            BindingFlags.Instance | BindingFlags.NonPublic);
        beginScope.Should().NotBeNull();
        flush.Should().NotBeNull();
        saveRequested.Should().NotBeNull();
        using var scope = beginScope!.Invoke(unitOfWork, null).Should().BeAssignableTo<IDisposable>().Subject;

        await ((ICompetitiveUnitOfWork)unitOfWork).SaveChangesAsync(CancellationToken.None);
        interceptor.Calls.Should().Be(0);
        saveRequested!.GetValue(unitOfWork).Should().Be(true);
        await flush!.Invoke(unitOfWork, [CancellationToken.None]).Should().BeAssignableTo<Task>().Subject;

        interceptor.Calls.Should().Be(1);
    }

    [Fact]
    public void CompetitiveUnitOfWork_DeferredScope_ShouldRejectNestingAndResetAfterDisposal()
    {
        using var context = CreateContext();
        var unitOfWork = new CompetitiveUnitOfWork(context);
        var beginScope = typeof(CompetitiveUnitOfWork).GetMethod(
            "BeginDeferredSaveScope",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var first = beginScope.Invoke(unitOfWork, null).Should().BeAssignableTo<IDisposable>().Subject;

        var nested = () => beginScope.Invoke(unitOfWork, null);

        nested.Should().Throw<TargetInvocationException>()
            .Where(exception => exception.InnerException is InvalidOperationException);
        first.Dispose();
        using var afterFailure = beginScope.Invoke(unitOfWork, null).Should().BeAssignableTo<IDisposable>().Subject;
    }

    private static RinhaDasLendasDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=competitive_model_tests;Username=test;Password=test")
            .AddInterceptors(interceptors);
        return new RinhaDasLendasDbContext(builder.Options);
    }

    private static Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter GetAuditSnapshotConverter(
        RinhaDasLendasDbContext context) =>
        context.Model.FindEntityType(typeof(RegistroAuditoriaCompetitiva))!
            .FindProperty(nameof(RegistroAuditoriaCompetitiva.ValorAnterior))!
            .GetTypeMapping().Converter!;

    private sealed class SuppressingSaveChangesInterceptor : SaveChangesInterceptor
    {
        internal DbContext? Context { get; private set; }
        internal int Calls { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Context = eventData.Context;
            Calls++;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }
    }
}
