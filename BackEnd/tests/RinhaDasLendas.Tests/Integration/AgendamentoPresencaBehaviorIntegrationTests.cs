using System.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Infrastructure.Time;

namespace RinhaDasLendas.Tests.Integration;

public sealed class AgendamentoPresencaBehaviorIntegrationTests
{
    private static readonly DateTimeOffset Agora = new(2026, 7, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ModeloEMigration_DevemPreservarTiposConstraintsEIndicesObrigatorios()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();

        var agenda = db.Model.FindEntityType(typeof(AgendamentoPresenca))!;
        var ocorrencia = db.Model.FindEntityType(typeof(OcorrenciaAgendamentoPresenca))!;
        var historico = db.Model.FindEntityType(typeof(HistoricoAgendamentoPresenca))!;

        ColumnType(agenda, nameof(AgendamentoPresenca.HorarioPublicacaoLocal)).Should().Be("time without time zone");
        ColumnType(agenda, nameof(AgendamentoPresenca.UltimaDataAvaliada)).Should().Be("date");
        ColumnType(agenda, nameof(AgendamentoPresenca.AtivadoEm)).Should().Be("timestamp with time zone");
        ColumnType(agenda, nameof(AgendamentoPresenca.Status)).Should().Be("smallint");
        ColumnType(ocorrencia, nameof(OcorrenciaAgendamentoPresenca.Status)).Should().Be("smallint");
        ColumnType(historico, nameof(HistoricoAgendamentoPresenca.Acao)).Should().Be("smallint");
        ColumnType(historico, nameof(HistoricoAgendamentoPresenca.CamposAlterados)).Should().Be("character varying(200)");

        var indexes = await ReadIndexDefinitionsAsync(db);
        indexes.Should().Contain(definition => definition.Contains("agendamento_presenca_id, data_local DESC", StringComparison.Ordinal));
        indexes.Should().Contain(definition => definition.Contains("draft_montagem_id", StringComparison.Ordinal)
            && definition.Contains("WHERE (draft_montagem_id IS NOT NULL)", StringComparison.Ordinal));
        indexes.Should().Contain(definition => definition.Contains("WHERE (status = 0)", StringComparison.Ordinal));
        indexes.Should().Contain(definition => definition.Contains("WHERE (status = 1)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Persistencia_DeveManterDiasRelacionaisHistoricoExatoEArquivamentoLogico()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var userId = await AddUserAsync(db);
        var agenda = CreateSchedule(userId, "Rinha de sexta", [DiaSemanaIso.Sexta, DiaSemanaIso.Domingo]);
        var repository = new AgendamentoPresencaRepository(db);

        await repository.AddAsync(agenda, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        db.ChangeTracker.Clear();

        var persisted = await repository.GetByIdAsync(agenda.Id, tracking: true, CancellationToken.None);
        persisted.Should().NotBeNull();
        persisted!.DiasSemana.Select(day => day.DiaSemana).Should().Equal(DiaSemanaIso.Sexta, DiaSemanaIso.Domingo);
        persisted.Historicos.Should().ContainSingle();
        persisted.Historicos.Single().CamposAlterados.Should().Be(
            "DiasSemana,HorarioEncerramentoLocal,HorarioPublicacaoLocal,Nome,Observacao,Status");

        persisted.Arquivar(userId, Agora.AddMinutes(1));
        await repository.SaveChangesAsync(CancellationToken.None);
        db.ChangeTracker.Clear();

        (await repository.GetByIdAsync(agenda.Id, tracking: false, CancellationToken.None))!.Status
            .Should().Be(AgendamentoPresencaStatus.Arquivado);
        (await repository.ListAsync(includePaused: true, page: 1, pageSize: 20, CancellationToken.None))
            .Should().NotContain(item => item.Id == agenda.Id);
    }

    [Fact]
    public async Task ListagensECounts_DevemPaginarSemDuplicarComEmpatesEPausadasPorUltimo()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var userId = await AddUserAsync(db);
        var repository = new AgendamentoPresencaRepository(db);
        var schedules = new[]
        {
            CreateSchedule(userId, "Beta", [DiaSemanaIso.Sexta]),
            CreateSchedule(userId, "Alfa", [DiaSemanaIso.Sexta]),
            CreateSchedule(userId, "Alfa", [DiaSemanaIso.Sexta]),
            CreateSchedule(userId, "Pausada", [DiaSemanaIso.Sexta]),
        };
        schedules[3].Pausar(userId, Agora.AddMinutes(1));
        foreach (var schedule in schedules)
        {
            await repository.AddAsync(schedule, CancellationToken.None);
        }
        await repository.SaveChangesAsync(CancellationToken.None);

        var firstPage = await repository.ListAsync(true, 1, 2, CancellationToken.None);
        var secondPage = await repository.ListAsync(true, 2, 2, CancellationToken.None);
        var combined = firstPage.Concat(secondPage).ToArray();
        var expected = schedules.Take(3).OrderBy(item => item.Nome).ThenBy(item => item.Id)
            .Concat([schedules[3]])
            .Select(item => item.Id);

        combined.Select(item => item.Id).Should().Equal(expected);
        combined.Select(item => item.Id).Should().OnlyHaveUniqueItems();
        (await repository.CountAsync(true, CancellationToken.None)).Should().Be(4);
        (await repository.CountAsync(false, CancellationToken.None)).Should().Be(3);

        var date = new DateOnly(2026, 7, 24);
        await repository.TryUpsertBlockedOccurrenceAsync(schedules[0].Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        await repository.TryUpsertBlockedOccurrenceAsync(schedules[0].Id, date.AddDays(1), Agora.AddDays(1), Agora.AddDays(1).AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);

        var occurrences = await repository.ListOccurrencesAsync(schedules[0].Id, 1, 1, CancellationToken.None);
        occurrences.Should().ContainSingle().Which.DataLocal.Should().Be(date.AddDays(1));
        (await repository.CountOccurrencesAsync(schedules[0].Id, CancellationToken.None)).Should().Be(2);
    }

    [Fact]
    public async Task DoisClaimsConcorrentes_DevemPersistirUmaOcorrenciaEUmVencedor()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid scheduleId;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            scheduleId = schedule.Id;
        }

        await using var dbA = database.CreateContext();
        await using var dbB = database.CreateContext();
        var repositoryA = new AgendamentoPresencaRepository(dbA);
        var repositoryB = new AgendamentoPresencaRepository(dbB);
        var claimA = Guid.NewGuid();
        var claimB = Guid.NewGuid();
        var date = new DateOnly(2026, 7, 24);

        var results = await Task.WhenAll(
            repositoryA.TryClaimOccurrenceAsync(scheduleId, date, Agora, Agora.AddHours(2), claimA, Agora.AddMinutes(5), Agora, CancellationToken.None),
            repositoryB.TryClaimOccurrenceAsync(scheduleId, date, Agora, Agora.AddHours(2), claimB, Agora.AddMinutes(5), Agora, CancellationToken.None));

        results.Should().NotContainNulls();
        results.Count(result => result!.Adquirido).Should().Be(1);
        results.Select(result => result!.OcorrenciaId).Distinct().Should().ContainSingle();
        await using var assertion = database.CreateContext();
        var occurrence = await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.ClaimId.Should().Be(results.Single(result => result!.Adquirido)!.ClaimId);
    }

    [Fact]
    public async Task ClaimExpirado_DeveSerRecuperavelSomenteComJanelaAberta()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        var firstClaimId = Guid.NewGuid();
        var secondClaimId = Guid.NewGuid();

        var first = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2), firstClaimId,
            Agora.AddMinutes(5), Agora, CancellationToken.None);
        var beforeExpiry = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2), secondClaimId,
            Agora.AddMinutes(9), Agora.AddMinutes(4), CancellationToken.None);
        var afterExpiry = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2), secondClaimId,
            Agora.AddMinutes(11), Agora.AddMinutes(6), CancellationToken.None);
        var afterClosure = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2), Guid.NewGuid(),
            Agora.AddHours(3), Agora.AddHours(2), CancellationToken.None);

        first!.Adquirido.Should().BeTrue();
        beforeExpiry!.Adquirido.Should().BeFalse();
        afterExpiry!.Adquirido.Should().BeTrue();
        afterExpiry.ClaimId.Should().Be(secondClaimId);
        afterClosure!.Adquirido.Should().BeFalse();
    }

    [Fact]
    public async Task CompareAndSet_DeveRejeitarClaimDivergenteSemAlterarOcorrencia()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), Guid.NewGuid(), Agora.AddMinutes(5), Agora, CancellationToken.None);
        var draft = CreateDraft(Agora.AddHours(2));

        var completed = await repository.TryCompleteWithDraftAsync(claim!.OcorrenciaId, Guid.NewGuid(), draft,
            Agora.AddMinutes(1), CancellationToken.None);
        var failed = await repository.TryMarkFailedAsync(claim.OcorrenciaId, Guid.NewGuid(),
            MessageCodes.PresenceScheduleTimeZoneInvalid, Agora.AddMinutes(1), CancellationToken.None);

        completed.Should().BeFalse();
        failed.Should().BeFalse();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Processando);
        occurrence.DraftMontagemId.Should().BeNull();
        (await db.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Conclusao_DevePersistirDraftPublicacaoEOcorrenciaAtomicamente()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var claimId = Guid.NewGuid();
        var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), claimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
        var draft = CreateDraft(Agora.AddHours(2));

        var completed = await repository.TryCompleteWithDraftAsync(claim!.OcorrenciaId, claimId, draft,
            Agora.AddMinutes(1), CancellationToken.None);

        completed.Should().BeTrue();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
        occurrence.DraftMontagemId.Should().Be(draft.Id);
        occurrence.ClaimId.Should().BeNull();
        occurrence.ClaimExpiresAt.Should().BeNull();
        (await db.DraftMontagens.AsNoTracking().CountAsync(item => item.Id == draft.Id)).Should().Be(1);
        var publication = await db.DraftMontagemPublicacoesDiscord.AsNoTracking().SingleAsync(item => item.DraftMontagemId == draft.Id);
        publication.Tipo.Should().Be(DraftMontagemPublicacaoDiscordTipo.Presenca);
        publication.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);
        publication.GuildId.Should().Be("guild-1");
    }

    [Fact]
    public async Task ConflitoAoInserirDraft_DeveFazerRollbackSemEstadoParcial()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var claimId = Guid.NewGuid();
        var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), claimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
        var conflictingDraft = CreateDraft(Agora.AddHours(2));
        db.DraftMontagens.Add(conflictingDraft);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var completed = await repository.TryCompleteWithDraftAsync(claim!.OcorrenciaId, claimId, conflictingDraft,
            Agora.AddMinutes(1), CancellationToken.None);

        completed.Should().BeFalse();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Processando);
        occurrence.DraftMontagemId.Should().BeNull();
        (await db.DraftMontagemPublicacoesDiscord.AsNoTracking()
            .CountAsync(item => item.DraftMontagemId == conflictingDraft.Id)).Should().Be(0);
    }

    [Fact]
    public async Task BloqueadasEPerdidas_DevemUsarUpsertSemReabrirEstadoTerminal()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);

        (await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None)).Should().BeTrue();
        (await repository.ListBlockedAsync(Agora, CancellationToken.None)).Should().ContainSingle();
        (await repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleWindowExpired, Agora.AddHours(2), CancellationToken.None)).Should().BeTrue();
        (await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora.AddHours(2), CancellationToken.None)).Should().BeFalse();

        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Perdida);
        occurrence.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleWindowExpired);
    }

    [Fact]
    public void Timezone_DeveConverterSaoPauloParaUtcESuaDataLocal()
    {
        IAgendamentoPresencaTimeZone timezone = new SaoPauloAgendamentoPresencaTimeZone();

        var instant = timezone.ToUtc(new DateOnly(2026, 7, 24), new TimeOnly(18, 0));

        instant.Should().Be(new DateTimeOffset(2026, 7, 24, 21, 0, 0, TimeSpan.Zero));
        timezone.GetLocalDate(instant).Should().Be(new DateOnly(2026, 7, 24));
    }

    [Theory]
    [InlineData(2018, 11, 4, 0, 30)]
    [InlineData(2019, 2, 16, 23, 30)]
    public void Timezone_NaoDeveAjustarHorarioLocalInvalidoOuAmbiguo(int year, int month, int day, int hour, int minute)
    {
        IAgendamentoPresencaTimeZone timezone = new SaoPauloAgendamentoPresencaTimeZone();

        var act = () => timezone.ToUtc(new DateOnly(year, month, day), new TimeOnly(hour, minute));

        act.Should().Throw<DomainException>().WithMessage(MessageCodes.PresenceScheduleTimeZoneInvalid);
    }

    private static string? ColumnType(IEntityType entityType, string propertyName) =>
        entityType.FindProperty(propertyName)?.GetColumnType();

    private static AgendamentoPresenca CreateSchedule(
        Guid userId,
        string name = "Rinha agendada",
        IReadOnlyCollection<DiaSemanaIso>? days = null) =>
        new(name, "Observacao", new TimeOnly(18, 0), new TimeOnly(20, 0), days ?? [DiaSemanaIso.Sexta],
            new DateOnly(2026, 7, 23), userId, Agora);

    private static DraftMontagem CreateDraft(DateTimeOffset closure)
    {
        var draft = new DraftMontagem("Rinha agendada - 24/07/2026", "Observacao", 5,
            DraftMontagemCriterioCapitaes.Manual, [], []);
        draft.ConfigurarEncerramentoPresenca(closure);
        draft.ConfigurarPublicacaoDiscord("guild-1", null);
        return draft;
    }

    private static async Task<Guid> AddUserAsync(RinhaDasLendasDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            Nome = "Responsavel",
            UserName = $"schedule-{userId:N}",
            NormalizedUserName = $"SCHEDULE-{userId:N}",
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private static async Task<IReadOnlyCollection<string>> ReadIndexDefinitionsAsync(RinhaDasLendasDbContext db)
    {
        var definitions = new List<string>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT indexdef
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename IN ('agendamentos_presenca', 'agendamentos_presenca_dias_semana', 'ocorrencias_agendamentos_presenca')
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            definitions.Add(reader.GetString(0));
        }

        return definitions;
    }

    private sealed class PostgreSqlTestDatabase : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _adminConnectionString;

        private PostgreSqlTestDatabase(string databaseName, string adminConnectionString, string connectionString)
        {
            _databaseName = databaseName;
            _adminConnectionString = adminConnectionString;
            ConnectionString = connectionString;
        }

        private string ConnectionString { get; }

        public static async Task<PostgreSqlTestDatabase> CreateAsync()
        {
            var adminConnectionString = $"Host={Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "localhost"};Port={Environment.GetEnvironmentVariable("TEST_POSTGRES_PORT") ?? "5432"};Database=postgres;Username=postgres;Password=postgres";
            var databaseName = $"rinha_schedule_{Guid.NewGuid():N}";
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
            var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
            var database = new PostgreSqlTestDatabase(databaseName, adminConnectionString, connectionString);
            await using var db = database.CreateContext();
            await db.Database.MigrateAsync();
            return database;
        }

        public RinhaDasLendasDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new RinhaDasLendasDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }
    }
}
