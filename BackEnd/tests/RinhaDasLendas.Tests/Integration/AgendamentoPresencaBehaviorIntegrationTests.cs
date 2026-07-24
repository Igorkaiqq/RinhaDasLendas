using System.Data;
using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Npgsql;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.AgendamentosPresenca;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Models;
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
    public async Task DoisHandlersConcorrentes_DevemPersistirUmEVincularOutroAoMV097()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid userId;
        Guid scheduleId;
        await using (var seed = database.CreateContext())
        {
            userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            scheduleId = schedule.Id;
        }

        var saveBarrier = new ConcurrentSaveBarrierInterceptor(expectedSaves: 2);
        await using var dbA = database.CreateContext(saveBarrier);
        await using var dbB = database.CreateContext(saveBarrier);
        var handlerA = CreateUpdateHandler(new AgendamentoPresencaRepository(dbA));
        var handlerB = CreateUpdateHandler(new AgendamentoPresencaRepository(dbB));
        var requestA = new SaveAgendamentoPresencaRequestDto(
            "Atualização A", null, [DiaSemanaIso.Sexta], new TimeOnly(18, 0), new TimeOnly(20, 0));
        var requestB = requestA with { Nome = "Atualização B" };

        var outcomes = await Task.WhenAll(
            CaptureExceptionAsync(async () => await handlerA.Handle(
                new UpdateAgendamentoPresencaCommand(scheduleId, requestA, userId), CancellationToken.None)),
            CaptureExceptionAsync(async () => await handlerB.Handle(
                new UpdateAgendamentoPresencaCommand(scheduleId, requestB, userId), CancellationToken.None)));

        outcomes.Count(exception => exception is null).Should().Be(1);
        outcomes.Count(exception => exception is DomainException domain
            && domain.MessageCode == MessageCodes.PresenceScheduleOccurrenceConflict).Should().Be(1);
        await using var assertion = database.CreateContext();
        var persisted = await assertion.AgendamentosPresenca.AsNoTracking().SingleAsync(item => item.Id == scheduleId);
        persisted.Nome.Should().BeOneOf("Atualização A", "Atualização B");
        (await assertion.HistoricosAgendamentosPresenca.AsNoTracking()
            .CountAsync(item => item.AgendamentoPresencaId == scheduleId)).Should().Be(2);
    }

    [Fact]
    public void Mapping_DeveUsarXminComoTokenDeConcorrenciaSemCampoDeDominio()
    {
        using var db = new RinhaDasLendasDbContext(new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options);
        var entity = db.Model.FindEntityType(typeof(AgendamentoPresenca));

        var xmin = entity!.FindProperty("xmin");
        xmin.Should().NotBeNull();
        xmin!.IsConcurrencyToken.Should().BeTrue();
        typeof(AgendamentoPresenca).GetProperty("xmin").Should().BeNull();
    }

    [Fact]
    public async Task ConsultasAdministrativas_DevemTerComandosConstantesEColecoesLimitadas()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid firstScheduleId;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedules = Enumerable.Range(0, 25).Select(index =>
                CreateSchedule(userId, $"Agenda {index:D2}", [DiaSemanaIso.Sexta])).ToArray();
            firstScheduleId = schedules[0].Id;
            foreach (var schedule in schedules)
            {
                for (var edit = 0; edit < 20; edit++)
                {
                    schedule.Editar(
                        schedule.Nome,
                        $"Observação {edit}",
                        schedule.HorarioPublicacaoLocal,
                        schedule.HorarioEncerramentoLocal,
                        [DiaSemanaIso.Sexta],
                        userId,
                        Agora.AddMinutes(edit + 1));
                }

                schedule.AdicionarOcorrencia(OcorrenciaAgendamentoPresenca.Bloqueada(
                    schedule.Id,
                    new DateOnly(2026, 7, 24),
                    Agora,
                    Agora.AddHours(2),
                    MessageCodes.PresenceScheduleDiscordUnavailable,
                    Agora,
                    schedule.Nome,
                    schedule.Observacao));
            }

            seed.AgendamentosPresenca.AddRange(schedules);
            await seed.SaveChangesAsync();
        }

        var counter = new CommandCounterInterceptor();
        await using var db = database.CreateContext(counter);
        var repository = new AgendamentoPresencaRepository(db);
        var list = await new ListAgendamentosPresencaQueryHandler(repository)
            .Handle(new ListAgendamentosPresencaQuery(1, 20), CancellationToken.None);

        counter.Count.Should().Be(4);
        list.Items.Should().HaveCount(20).And.OnlyContain(item => item.UltimaOcorrencia != null);

        counter.Reset();
        var occurrences = await new ListOcorrenciasAgendamentoPresencaQueryHandler(repository)
            .Handle(new ListOcorrenciasAgendamentoPresencaQuery(firstScheduleId, 1, 20), CancellationToken.None);
        counter.Count.Should().Be(3);
        occurrences.Should().NotBeNull();

        db.ChangeTracker.Clear();
        var loaded = await repository.GetByIdAsync(firstScheduleId, tracking: true, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.DiasSemana.Should().ContainSingle();
        loaded.Historicos.Should().BeEmpty();
        loaded.Ocorrencias.Should().BeEmpty();
    }

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
        indexes.Should().Contain(definition => definition.Contains("WHERE (status = ANY (ARRAY[0, 1]))", StringComparison.Ordinal));
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
        persisted.Historicos.Should().BeEmpty();
        var creationHistory = await db.HistoricosAgendamentosPresenca.AsNoTracking()
            .SingleAsync(item => item.AgendamentoPresencaId == agenda.Id);
        creationHistory.CamposAlterados.Should().Be(
            "DiasSemana,HorarioEncerramentoLocal,HorarioPublicacaoLocal,Nome,Observacao,Status");

        persisted.Arquivar(userId, Agora.AddMinutes(1));
        await repository.SaveChangesAsync(CancellationToken.None);
        db.ChangeTracker.Clear();
        (await db.HistoricosAgendamentosPresenca.AsNoTracking()
            .CountAsync(item => item.AgendamentoPresencaId == agenda.Id)).Should().Be(2);

        (await repository.GetByIdAsync(agenda.Id, tracking: false, CancellationToken.None))!.Status
            .Should().Be(AgendamentoPresencaStatus.Arquivado);
        (await repository.ListAsync(includePaused: true, page: 1, pageSize: 20, CancellationToken.None))
            .Should().NotContain(item => item.Agenda.Id == agenda.Id);
    }

    [Fact]
    public async Task FactoryProcessando_DeveProduzirEntidadePersistivelComClaimCompleto()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        db.AgendamentosPresenca.Add(schedule);
        await db.SaveChangesAsync();
        var claimId = Guid.NewGuid();
        var occurrence = OcorrenciaAgendamentoPresenca.Processando(
            schedule.Id,
            new DateOnly(2026, 7, 24),
            Agora,
            Agora.AddHours(2),
            claimId,
            Agora.AddMinutes(5),
            Agora,
            "  Snapshot persistido  ",
            "  Observacao persistida  ");

        db.OcorrenciasAgendamentosPresenca.Add(occurrence);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        persisted.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Processando);
        persisted.ClaimId.Should().Be(claimId);
        persisted.ClaimExpiresAt.Should().Be(Agora.AddMinutes(5));
        persisted.NomeSnapshot.Should().Be("Snapshot persistido");
        persisted.ObservacaoSnapshot.Should().Be("Observacao persistida");
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

        combined.Select(item => item.Agenda.Id).Should().Equal(expected);
        combined.Select(item => item.Agenda.Id).Should().OnlyHaveUniqueItems();
        combined.Take(3).Should().OnlyContain(item =>
            item.ProximaExecucaoEm == new DateTimeOffset(2026, 7, 24, 18, 0, 0, TimeSpan.Zero));
        combined.Last().ProximaExecucaoEm.Should().BeNull();
        (await repository.CountAsync(true, CancellationToken.None)).Should().Be(4);
        (await repository.CountAsync(false, CancellationToken.None)).Should().Be(3);

        var date = new DateOnly(2026, 7, 24);
        await repository.TryUpsertBlockedOccurrenceAsync(schedules[0].Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        await repository.TryUpsertBlockedOccurrenceAsync(schedules[0].Id, date.AddDays(7), Agora.AddDays(7), Agora.AddDays(7).AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);

        var occurrences = await repository.ListOccurrencesAsync(schedules[0].Id, 1, 1, CancellationToken.None);
        occurrences.Should().ContainSingle().Which.DataLocal.Should().Be(date.AddDays(7));
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
            Agora.AddHours(2).AddMinutes(5), Agora.AddHours(2), CancellationToken.None);

        first!.Adquirido.Should().BeTrue();
        beforeExpiry!.Adquirido.Should().BeFalse();
        afterExpiry!.Adquirido.Should().BeTrue();
        afterExpiry.ClaimId.Should().Be(secondClaimId);
        afterClosure!.Adquirido.Should().BeFalse();
    }

    [Theory]
    [InlineData(OcorrenciaAgendamentoPresencaStatus.Criada)]
    [InlineData(OcorrenciaAgendamentoPresencaStatus.Perdida)]
    [InlineData(OcorrenciaAgendamentoPresencaStatus.Falha)]
    public async Task TryClaim_EmOcorrenciaTerminalSemClaim_DeveRetornarNaoAdquirido(
        OcorrenciaAgendamentoPresencaStatus terminalStatus)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);

        if (terminalStatus == OcorrenciaAgendamentoPresencaStatus.Perdida)
        {
            await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
            await repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                MessageCodes.PresenceScheduleWindowExpired, Agora.AddHours(2), CancellationToken.None);
        }
        else
        {
            var ownerClaimId = Guid.NewGuid();
            var ownerClaim = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                ownerClaimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
            if (terminalStatus == OcorrenciaAgendamentoPresencaStatus.Criada)
            {
                await repository.TryCompleteWithDraftAsync(ownerClaim!.OcorrenciaId, ownerClaimId,
                    CreateDraft(Agora.AddHours(2)), Agora.AddMinutes(1), CancellationToken.None);
            }
            else
            {
                await repository.TryMarkFailedAsync(ownerClaim!.OcorrenciaId, ownerClaimId,
                    MessageCodes.PresenceScheduleTimeZoneInvalid, Agora.AddMinutes(1), CancellationToken.None);
            }
        }

        var attemptNow = Agora.AddMinutes(2);
        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora.AddHours(-1), Agora.AddHours(3),
            Guid.NewGuid(), attemptNow.AddMinutes(5), attemptNow, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Adquirido.Should().BeFalse();
        result.ClaimId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task TryClaim_AposBloqueio_DeveUsarJanelaPersistidaSemSobrescreveLa()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        var persistedPublication = Agora;
        var persistedClosure = Agora.AddHours(2);
        await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, persistedPublication, persistedClosure,
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        var attemptNow = Agora.AddMinutes(30);

        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora.AddHours(10), Agora.AddHours(12),
            Guid.NewGuid(), attemptNow.AddMinutes(5), attemptNow, CancellationToken.None);

        result!.Adquirido.Should().BeTrue();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.PublicacaoPrevistaEm.Should().Be(persistedPublication);
        occurrence.EncerramentoPrevistoEm.Should().Be(persistedClosure);
    }

    [Fact]
    public async Task TryClaim_AposExpiracao_DeveUsarJanelaPersistidaSemSobrescreveLa()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        var persistedPublication = Agora;
        var persistedClosure = Agora.AddHours(2);
        await repository.TryClaimOccurrenceAsync(schedule.Id, date, persistedPublication, persistedClosure,
            Guid.NewGuid(), Agora.AddMinutes(5), Agora, CancellationToken.None);
        var attemptNow = Agora.AddMinutes(6);

        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora.AddHours(10), Agora.AddHours(12),
            Guid.NewGuid(), attemptNow.AddMinutes(5), attemptNow, CancellationToken.None);

        result!.Adquirido.Should().BeTrue();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.PublicacaoPrevistaEm.Should().Be(persistedPublication);
        occurrence.EncerramentoPrevistoEm.Should().Be(persistedClosure);
    }

    [Fact]
    public async Task TryUpsertMissed_DeveMarcarProcessandoExpiradoComoPerdidoSemTocarTerminal()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        var claimId = Guid.NewGuid();
        await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            claimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
        var missedAt = Agora.AddHours(2);

        var changed = await repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleWindowExpired, missedAt, CancellationToken.None);

        changed.Changed.Should().BeTrue();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Perdida);
        occurrence.ClaimId.Should().BeNull();
        occurrence.ClaimExpiresAt.Should().BeNull();
        occurrence.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleWindowExpired);
        var updatedAt = occurrence.AtualizadaEm;

        (await repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora.AddHours(-1), Agora.AddHours(3),
            MessageCodes.PresenceScheduleWindowExpired, missedAt.AddMinutes(1), CancellationToken.None)).Changed.Should().BeFalse();
        db.ChangeTracker.Clear();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync()).AtualizadaEm.Should().Be(updatedAt);
    }

    [Fact]
    public async Task TryClaim_PrimeiraAquisicaoComConfiguracaoObsoleta_NaoDeveCriarOcorrencia()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        schedule.Editar("Rinha editada", null, new TimeOnly(16, 0), new TimeOnly(18, 0),
            [DiaSemanaIso.Sexta], userId, Agora.AddMinutes(1));
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24),
            Agora, Agora.AddHours(2), Guid.NewGuid(), Agora.AddMinutes(5), Agora, CancellationToken.None);

        result.Should().BeNull();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TryClaim_PrimeiraAquisicaoEmDiaNaoConfigurado_NaoDeveCriarOcorrencia()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId, days: [DiaSemanaIso.Sabado]);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24),
            Agora, Agora.AddHours(2), Guid.NewGuid(), Agora.AddMinutes(5), Agora, CancellationToken.None);

        result.Should().BeNull();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TryClaim_BloqueadaExistente_DeveIgnorarEdicaoPosteriorDaAgenda()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        schedule.Editar("Rinha editada", null, new TimeOnly(16, 0), new TimeOnly(18, 0),
            [DiaSemanaIso.Sabado], userId, Agora.AddMinutes(1));
        await repository.SaveChangesAsync(CancellationToken.None);
        var claimNow = Agora.AddMinutes(30);

        var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora.AddHours(10), Agora.AddHours(12),
            Guid.NewGuid(), claimNow.AddMinutes(5), claimNow, CancellationToken.None);

        claim!.Adquirido.Should().BeTrue();
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.PublicacaoPrevistaEm.Should().Be(Agora);
        occurrence.EncerramentoPrevistoEm.Should().Be(Agora.AddHours(2));
    }

    [Theory]
    [InlineData("paused")]
    [InlineData("archived")]
    public async Task TryClaim_BloqueadaExistente_DeveIgnorarStatusPosteriorDaAgendaEConcluir(string status)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        if (status == "paused")
        {
            schedule.Pausar(userId, Agora.AddMinutes(1));
        }
        else
        {
            schedule.Arquivar(userId, Agora.AddMinutes(1));
        }
        await repository.SaveChangesAsync(CancellationToken.None);
        var claimNow = Agora.AddMinutes(30);
        var claimId = Guid.NewGuid();

        var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora.AddHours(10), Agora.AddHours(12),
            claimId, claimNow.AddMinutes(5), claimNow, CancellationToken.None);

        claim!.Adquirido.Should().BeTrue();
        (await repository.TryCompleteWithDraftAsync(claim.OcorrenciaId, claimId, CreateDraft(Agora.AddHours(2)),
            claimNow.AddMinutes(1), CancellationToken.None)).Should().BeTrue();
        db.ChangeTracker.Clear();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync()).Status
            .Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
    }

    [Fact]
    public async Task TryClaim_AgendaPausadaSemOcorrencia_NaoDeveCriar()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        schedule.Pausar(userId, Agora.AddMinutes(1));
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24),
            Agora, Agora.AddHours(2), Guid.NewGuid(), Agora.AddMinutes(5), Agora, CancellationToken.None);

        result.Should().BeNull();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("blocked", "stale-times")]
    [InlineData("blocked", "removed-day")]
    [InlineData("blocked", "paused")]
    [InlineData("missed", "stale-times")]
    [InlineData("missed", "removed-day")]
    [InlineData("missed", "paused")]
    public async Task Upsert_PrimeiraCriacaoComConfiguracaoInvalida_NaoDeveCriar(
        string operation,
        string invalidConfiguration)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        if (invalidConfiguration == "stale-times")
        {
            schedule.Editar("Rinha editada", null, new TimeOnly(16, 0), new TimeOnly(18, 0),
                [DiaSemanaIso.Sexta], userId, Agora.AddMinutes(1));
        }
        else if (invalidConfiguration == "removed-day")
        {
            schedule.Editar("Rinha editada", null, new TimeOnly(15, 0), new TimeOnly(17, 0),
                [DiaSemanaIso.Sabado], userId, Agora.AddMinutes(1));
        }
        else
        {
            schedule.Pausar(userId, Agora.AddMinutes(1));
        }
        await repository.SaveChangesAsync(CancellationToken.None);

        var changed = await ExecuteOccurrenceUpsertAsync(
            repository,
            operation,
            schedule.Id,
            new DateOnly(2026, 7, 24),
            Agora,
            Agora.AddHours(2),
            operation == "blocked" ? Agora : Agora.AddHours(2));

        changed.Should().BeFalse();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("blocked", "paused")]
    [InlineData("blocked", "archived")]
    [InlineData("missed", "paused")]
    [InlineData("missed", "archived")]
    public async Task Upsert_OcorrenciaExistente_DeveUsarEstadoEJanelaPersistidos(
        string operation,
        string scheduleStatus)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);
        var persistedPublication = Agora;
        var persistedClosure = Agora.AddHours(2);
        await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, persistedPublication, persistedClosure,
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        schedule.Editar("Rinha editada", null, new TimeOnly(16, 0), new TimeOnly(18, 0),
            [DiaSemanaIso.Sabado], userId, Agora.AddMinutes(1));
        if (scheduleStatus == "paused")
        {
            schedule.Pausar(userId, Agora.AddMinutes(2));
        }
        else
        {
            schedule.Arquivar(userId, Agora.AddMinutes(2));
        }
        await repository.SaveChangesAsync(CancellationToken.None);

        var changed = await ExecuteOccurrenceUpsertAsync(
            repository,
            operation,
            schedule.Id,
            date,
            Agora.AddHours(10),
            Agora.AddHours(12),
            operation == "blocked" ? Agora.AddMinutes(30) : persistedClosure);

        changed.Should().Be(operation == "missed");
        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.PublicacaoPrevistaEm.Should().Be(persistedPublication);
        occurrence.EncerramentoPrevistoEm.Should().Be(persistedClosure);
        occurrence.Status.Should().Be(operation == "missed"
            ? OcorrenciaAgendamentoPresencaStatus.Perdida
            : OcorrenciaAgendamentoPresencaStatus.Bloqueada);
    }

    [Theory]
    [InlineData("blocked", MessageCodes.PresenceScheduleDiscordUnavailable)]
    [InlineData("missed", MessageCodes.PresenceScheduleWindowExpired)]
    [InlineData("failed", MessageCodes.PresenceScheduleTimeZoneInvalid)]
    public async Task OperacoesSql_DevemPersistirCodigoCanonicoQuandoEntradaTemEspacos(
        string operation,
        string expectedCode)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);

        if (operation == "blocked")
        {
            await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                $" {expectedCode} ", Agora, CancellationToken.None);
        }
        else if (operation == "missed")
        {
            await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
            await repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                $" {expectedCode} ", Agora.AddHours(2), CancellationToken.None);
        }
        else
        {
            var claimId = Guid.NewGuid();
            var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                claimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
            await repository.TryMarkFailedAsync(claim!.OcorrenciaId, claimId,
                $" {expectedCode} ", Agora.AddMinutes(1), CancellationToken.None);
        }

        db.ChangeTracker.Clear();
        (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync()).CodigoFalha.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData(-300)]
    [InlineData(299)]
    [InlineData(301)]
    public async Task TryClaim_DeveRejeitarTtlDiferenteDeCincoMinutos(int ttlSeconds)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var act = () => repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), Guid.NewGuid(), Agora.AddSeconds(ttlSeconds), Agora, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(MessageCodes.PresenceScheduleOccurrenceConflict);
    }

    [Fact]
    public async Task TryClaim_DeveRejeitarClaimIdVazio()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var act = () => repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), Guid.Empty, Agora.AddMinutes(5), Agora, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(MessageCodes.PresenceScheduleOccurrenceConflict);
    }

    [Fact]
    public async Task TryClaim_DeveAceitarTtlComDiferencaAbaixoDaPrecisaoDoPostgreSql()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), Guid.NewGuid(), Agora.AddMinutes(5).AddTicks(9), Agora, CancellationToken.None);

        result!.Adquirido.Should().BeTrue();
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("missed")]
    [InlineData("failed")]
    public async Task OperacoesSql_DevemRejeitarCodigoIncompativelComATransicao(string operation)
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var date = new DateOnly(2026, 7, 24);

        Func<Task> act;
        if (operation == "blocked")
        {
            act = () => repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                MessageCodes.PresenceScheduleWindowExpired, Agora, CancellationToken.None);
        }
        else if (operation == "missed")
        {
            act = () => repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                MessageCodes.PresenceScheduleDiscordUnavailable, Agora.AddHours(2), CancellationToken.None);
        }
        else
        {
            var claimId = Guid.NewGuid();
            var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
                claimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
            act = () => repository.TryMarkFailedAsync(claim!.OcorrenciaId, claimId,
                MessageCodes.PresenceScheduleWindowExpired, Agora.AddMinutes(1), CancellationToken.None);
        }

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage(MessageCodes.PresenceScheduleOccurrenceConflict);
    }

    [Fact]
    public async Task ListagensDeAgendas_DevemMaterializarSomenteDiasSemana()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid scheduleId;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            var repository = new AgendamentoPresencaRepository(seed);
            await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
                Agora.AddHours(2), MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
            scheduleId = schedule.Id;
        }

        await using var db = database.CreateContext();
        var repositoryForList = new AgendamentoPresencaRepository(db);
        var listed = (await repositoryForList.ListAsync(true, 1, 20, CancellationToken.None)).Single(item => item.Agenda.Id == scheduleId).Agenda;
        var candidate = (await repositoryForList.ListCandidatesAsync(Agora, null, 20, CancellationToken.None))
            .Single(item => item.Id == scheduleId);

        listed.DiasSemana.Should().ContainSingle();
        listed.Historicos.Should().BeEmpty();
        listed.Ocorrencias.Should().BeEmpty();
        candidate.DiasSemana.Should().ContainSingle();
        candidate.Historicos.Should().BeEmpty();
        candidate.Ocorrencias.Should().BeEmpty();
    }

    [Fact]
    public async Task Constraints_DevemRejeitarCombinacoesInvalidasDeClaimEDraftPorStatus()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid scheduleId;
        Guid draftId;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            var draft = CreateDraft(Agora.AddHours(2));
            seed.AgendamentosPresenca.Add(schedule);
            seed.DraftMontagens.Add(draft);
            await seed.SaveChangesAsync();
            scheduleId = schedule.Id;
            draftId = draft.Id;
        }

        var invalidStates = new[]
        {
            new InvalidOccurrenceState(0, null, null, null),
            new InvalidOccurrenceState(1, Guid.NewGuid(), Agora.AddMinutes(5), null),
            new InvalidOccurrenceState(2, null, null, null),
            new InvalidOccurrenceState(1, null, null, draftId),
            new InvalidOccurrenceState(3, null, null, draftId),
            new InvalidOccurrenceState(4, null, null, draftId),
        };

        foreach (var state in invalidStates)
        {
            var act = () => InsertOccurrenceStateAsync(database, scheduleId, state);
            await act.Should().ThrowAsync<PostgresException>()
                .Where(exception => exception.SqlState == PostgresErrorCodes.CheckViolation);
        }
    }

    [Fact]
    public async Task ListBlocked_DeveIgnorarMarcadorDaAgendaEManterOcorrenciaAposEncerramento()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId);
        await repository.AddAsync(schedule, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
            Agora.AddHours(2), MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
        schedule.MarcarDataAvaliada(new DateOnly(2026, 7, 30), Agora.AddHours(1));
        await repository.SaveChangesAsync(CancellationToken.None);

        var blocked = await repository.ListBlockedAsync(Agora.AddHours(3), 20, CancellationToken.None);

        blocked.Should().ContainSingle(item => item.AgendamentoPresencaId == schedule.Id);
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
        draft.PublicacoesDiscord.Should().ContainSingle(item => item.Id == publication.Id);
    }

    [Fact]
    public void DraftMontagem_DeveCriarPublicacaoPendentePeloAgregadoComInstanteExplicito()
    {
        var draft = new DraftMontagem("Rinha agendada - 24/07/2026", null, 5,
            DraftMontagemCriterioCapitaes.Manual, [], []);

        var publication = draft.ConfigurarPublicacaoDiscordPendente(
            DraftMontagemPublicacaoDiscordTipo.Presenca,
            " guild-1 ",
            " channel-1 ",
            Agora);

        publication.Tipo.Should().Be(DraftMontagemPublicacaoDiscordTipo.Presenca);
        publication.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);
        publication.GuildId.Should().Be("guild-1");
        publication.ChannelId.Should().Be("channel-1");
        publication.UltimaTentativaEm.Should().Be(Agora);
        draft.PublicacoesDiscord.Should().ContainSingle().Which.Should().BeSameAs(publication);

        var manualDraft = new DraftMontagem("Draft manual", null, 5,
            DraftMontagemCriterioCapitaes.Manual, [], []);
        manualDraft.ConfigurarPublicacaoDiscord("guild-manual", null);
        manualDraft.PublicacoesDiscord.Should().BeEmpty();
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
    public async Task FalhaAposPersistirDraftEPublicacao_DeveFazerRollbackLimparTrackingEPermitirReusoDoContexto()
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
        var occurrenceId = claim!.OcorrenciaId;
        db.ChangeTracker.Clear();
        await ExecuteSqlAsync(database, """
            CREATE FUNCTION fail_scheduled_occurrence_completion() RETURNS trigger AS $$
            BEGIN
                IF NEW.status = 2 THEN
                    RAISE EXCEPTION 'injected completion failure' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER fail_scheduled_occurrence_completion
            BEFORE UPDATE ON ocorrencias_agendamentos_presenca
            FOR EACH ROW EXECUTE FUNCTION fail_scheduled_occurrence_completion();
            """);

        var act = () => repository.TryCompleteWithDraftAsync(occurrenceId, claimId,
            CreateDraft(Agora.AddHours(2)), Agora.AddMinutes(1), CancellationToken.None);

        await act.Should().ThrowAsync<PostgresException>()
            .Where(exception => exception.SqlState == PostgresErrorCodes.CheckViolation);
        db.ChangeTracker.Entries().Should().BeEmpty();
        await ExecuteSqlAsync(database, """
            DROP TRIGGER fail_scheduled_occurrence_completion ON ocorrencias_agendamentos_presenca;
            DROP FUNCTION fail_scheduled_occurrence_completion();
            """);
        await using (var assertion = database.CreateContext())
        {
            (await assertion.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(0);
            (await assertion.DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync()).Should().Be(0);
            (await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync()).Status
                .Should().Be(OcorrenciaAgendamentoPresencaStatus.Processando);
        }

        var retried = await repository.TryCompleteWithDraftAsync(occurrenceId, claimId,
            CreateDraft(Agora.AddHours(2)), Agora.AddMinutes(2), CancellationToken.None);

        retried.Should().BeTrue();
    }

    [Fact]
    public async Task DuasConclusoesConcorrentes_DevemPersistirUmDraftEUmaPublicacao()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid occurrenceId;
        Guid claimId;
        await using (var seed = database.CreateContext())
        {
            var repository = new AgendamentoPresencaRepository(seed);
            var userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            await repository.AddAsync(schedule, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);
            claimId = Guid.NewGuid();
            var claim = await repository.TryClaimOccurrenceAsync(schedule.Id, new DateOnly(2026, 7, 24), Agora,
                Agora.AddHours(2), claimId, Agora.AddMinutes(5), Agora, CancellationToken.None);
            occurrenceId = claim!.OcorrenciaId;
        }

        await using var dbA = database.CreateContext();
        await using var dbB = database.CreateContext();
        var repositoryA = new AgendamentoPresencaRepository(dbA);
        var repositoryB = new AgendamentoPresencaRepository(dbB);

        var results = await Task.WhenAll(
            repositoryA.TryCompleteWithDraftAsync(occurrenceId, claimId, CreateDraft(Agora.AddHours(2)),
                Agora.AddMinutes(1), CancellationToken.None),
            repositoryB.TryCompleteWithDraftAsync(occurrenceId, claimId, CreateDraft(Agora.AddHours(2)),
                Agora.AddMinutes(1), CancellationToken.None));

        results.Should().ContainSingle(result => result);
        await using var assertion = database.CreateContext();
        (await assertion.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(1);
        (await assertion.DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync()).Should().Be(1);
        var occurrence = await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
        occurrence.DraftMontagemId.Should().NotBeNull();
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
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None)).Changed.Should().BeTrue();
        (await repository.ListBlockedAsync(Agora, 20, CancellationToken.None)).Should().ContainSingle();
        (await repository.TryUpsertMissedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleWindowExpired, Agora.AddHours(2), CancellationToken.None)).Changed.Should().BeTrue();
        (await repository.TryUpsertBlockedOccurrenceAsync(schedule.Id, date, Agora, Agora.AddHours(2),
            MessageCodes.PresenceScheduleDiscordUnavailable, Agora.AddHours(2), CancellationToken.None)).Changed.Should().BeFalse();

        db.ChangeTracker.Clear();
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Perdida);
        occurrence.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleWindowExpired);
    }

    [Fact]
    public async Task CiclosDoHandler_DevemCriarUmaOcorrenciaUmDraftEUmaPublicacaoPendente()
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

        await using (var firstContext = database.CreateContext())
        {
            var first = CreateExecutionHandler(new AgendamentoPresencaRepository(firstContext));
            var result = await first.Handle(new ProcessarAgendamentosPresencaDevidosCommand(Agora), CancellationToken.None);
            result.Criadas.Should().Be(1);
        }

        await using (var secondContext = database.CreateContext())
        {
            var second = CreateExecutionHandler(new AgendamentoPresencaRepository(secondContext));
            var result = await second.Handle(new ProcessarAgendamentosPresencaDevidosCommand(Agora.AddMinutes(1)), CancellationToken.None);
            result.Criadas.Should().Be(0);
        }

        await using var assertion = database.CreateContext();
        (await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(1);
        (await assertion.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(1);
        (await assertion.DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync()).Should().Be(1);
        var occurrence = await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.AgendamentoPresencaId.Should().Be(scheduleId);
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
    }

    [Fact]
    public async Task HandlersConcorrentes_DevemCriarSomenteUmDraftEManterMarcadorRecuperavel()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            seed.AgendamentosPresenca.Add(CreateSchedule(userId));
            await seed.SaveChangesAsync();
        }

        await using var dbA = database.CreateContext();
        await using var dbB = database.CreateContext();
        var results = await Task.WhenAll(
            CreateExecutionHandler(new AgendamentoPresencaRepository(dbA)).Handle(
                new ProcessarAgendamentosPresencaDevidosCommand(Agora), CancellationToken.None),
            CreateExecutionHandler(new AgendamentoPresencaRepository(dbB)).Handle(
                new ProcessarAgendamentosPresencaDevidosCommand(Agora), CancellationToken.None));

        results.Sum(item => item.Criadas).Should().Be(1);
        await using var assertion = database.CreateContext();
        (await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(1);
        (await assertion.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(1);
        (await assertion.DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handler_DeveReadquirirClaimExpiradoDentroDaJanela()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            var repository = new AgendamentoPresencaRepository(seed);
            await repository.TryClaimOccurrenceAsync(
                schedule.Id, new DateOnly(2026, 7, 24), Agora, Agora.AddHours(2), Guid.NewGuid(),
                Agora.AddMinutes(5), Agora, CancellationToken.None);
        }

        var retryAt = Agora.AddMinutes(6);
        await using (var execution = database.CreateContext())
        {
            var result = await CreateExecutionHandler(
                    new AgendamentoPresencaRepository(execution), retryAt)
                .Handle(new ProcessarAgendamentosPresencaDevidosCommand(retryAt), CancellationToken.None);
            result.Criadas.Should().Be(1);
        }

        await using var assertion = database.CreateContext();
        var occurrence = await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
        occurrence.ClaimId.Should().BeNull();
    }

    [Fact]
    public async Task ConflitoXminNoPrimeiroMarcador_DeveLimparTrackingEPersistirSegundaAgenda()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            seed.AgendamentosPresenca.AddRange(
                CreateSchedule(userId, "Agenda A"),
                CreateSchedule(userId, "Agenda B"));
            await seed.SaveChangesAsync();
        }

        Guid firstId;
        Guid secondId;
        await using (var ordering = database.CreateContext())
        {
            var ordered = await new AgendamentoPresencaRepository(ordering)
                .ListCandidatesAsync(Agora, null, 10, CancellationToken.None);
            firstId = ordered.First().Id;
            secondId = ordered.Last().Id;
        }

        var interceptor = new BumpScheduleXminOnFirstSaveInterceptor(database, firstId);
        await using (var execution = database.CreateContext(interceptor))
        {
            var unavailable = new Mock<IDiscordConfigurationService>();
            unavailable.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((DiscordConfigurationDto?)null);
            var handler = CreateExecutionHandler(
                new AgendamentoPresencaRepository(execution), Agora, unavailable.Object);

            var result = await handler.Handle(
                new ProcessarAgendamentosPresencaDevidosCommand(Agora), CancellationToken.None);

            result.Falhas.Should().Be(1);
            result.Bloqueadas.Should().Be(2);
        }

        await using var assertion = database.CreateContext();
        var schedules = await assertion.AgendamentosPresenca.AsNoTracking().ToDictionaryAsync(item => item.Id);
        schedules[firstId].UltimaDataAvaliada.Should().Be(new DateOnly(2026, 7, 23));
        schedules[secondId].UltimaDataAvaliada.Should().Be(new DateOnly(2026, 7, 24));
    }

    [Fact]
    public async Task ListCandidates_DeveExcluirPublicacaoFuturaERotacionarAposCursor()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            seed.AgendamentosPresenca.AddRange(
                new AgendamentoPresenca("Futura", null, new TimeOnly(16, 0), new TimeOnly(17, 0),
                    [DiaSemanaIso.Sexta], new DateOnly(2026, 7, 23), userId, Agora),
                new AgendamentoPresenca("Devida A", null, new TimeOnly(14, 0), new TimeOnly(17, 0),
                    [DiaSemanaIso.Sexta], new DateOnly(2026, 7, 23), userId, Agora),
                new AgendamentoPresenca("Devida B", null, new TimeOnly(14, 0), new TimeOnly(17, 0),
                    [DiaSemanaIso.Sexta], new DateOnly(2026, 7, 23), userId, Agora),
                new AgendamentoPresenca("Devida curta", null, new TimeOnly(14, 0), new TimeOnly(15, 1),
                    [DiaSemanaIso.Sexta], new DateOnly(2026, 7, 23), userId, Agora));
            await seed.SaveChangesAsync();
        }

        await using var db = database.CreateContext();
        var repository = new AgendamentoPresencaRepository(db);
        var first = await repository.ListCandidatesAsync(Agora, null, 2, CancellationToken.None);
        var second = await repository.ListCandidatesAsync(Agora, first.Last().Id, 2, CancellationToken.None);

        first.Should().HaveCount(2).And.NotContain(item => item.Nome == "Futura");
        second.Should().NotContain(item => item.Nome == "Futura");
        first.Concat(second).Should().Contain(item => item.Nome == "Devida curta");
    }

    [Fact]
    public async Task BloqueadaComMarcadorAvancado_DeveSerRecuperadaPeloHandler()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid scheduleId;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedule = CreateSchedule(userId);
            schedule.MarcarDataAvaliada(new DateOnly(2026, 7, 24), Agora);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            var repository = new AgendamentoPresencaRepository(seed);
            (await repository.TryUpsertBlockedOccurrenceAsync(
                schedule.Id,
                new DateOnly(2026, 7, 24),
                Agora,
                Agora.AddHours(2),
                MessageCodes.PresenceScheduleDiscordUnavailable,
                Agora,
                CancellationToken.None)).Changed.Should().BeTrue();
            scheduleId = schedule.Id;
        }

        await using (var execution = database.CreateContext())
        {
            var handler = CreateExecutionHandler(new AgendamentoPresencaRepository(execution));
            var result = await handler.Handle(
                new ProcessarAgendamentosPresencaDevidosCommand(Agora.AddMinutes(30)), CancellationToken.None);
            result.Criadas.Should().Be(1);
        }

        await using var assertion = database.CreateContext();
        var occurrence = await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.AgendamentoPresencaId.Should().Be(scheduleId);
        occurrence.Status.Should().Be(OcorrenciaAgendamentoPresencaStatus.Criada);
        (await assertion.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(1);
        (await assertion.DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task LoteDeBloqueadasDeveSerLimitadoESemStarvationEntreCiclos()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedules = Enumerable.Range(1, 3)
                .Select(index => CreateSchedule(userId, $"Agenda {index}"))
                .ToArray();
            foreach (var schedule in schedules)
            {
                schedule.MarcarDataAvaliada(new DateOnly(2026, 7, 24), Agora);
            }

            seed.AgendamentosPresenca.AddRange(schedules);
            await seed.SaveChangesAsync();
            var repository = new AgendamentoPresencaRepository(seed);
            foreach (var schedule in schedules)
            {
                await repository.TryUpsertBlockedOccurrenceAsync(
                    schedule.Id, new DateOnly(2026, 7, 24), Agora, Agora.AddHours(2),
                    MessageCodes.PresenceScheduleDiscordUnavailable, Agora, CancellationToken.None);
            }
        }

        var options = new AgendamentoPresencaProcessingOptions
        {
            MaxBlockedPerCycle = 1,
            MaxSchedulesPerCycle = 1,
            MaxDatesPerSchedulePerCycle = 1
        };
        for (var cycle = 0; cycle < 3; cycle++)
        {
            await using var execution = database.CreateContext();
            var result = await CreateExecutionHandler(
                    new AgendamentoPresencaRepository(execution), Agora.AddMinutes(1), options: options)
                .Handle(new ProcessarAgendamentosPresencaDevidosCommand(Agora), CancellationToken.None);
            result.Criadas.Should().Be(1);
        }

        await using var assertion = database.CreateContext();
        (await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking()
            .CountAsync(item => item.Status == OcorrenciaAgendamentoPresencaStatus.Criada)).Should().Be(3);
    }

    [Fact]
    public async Task TimezoneInvalido_DevePersistirFalhaIdempotenteComJanelaDeterministicaDoPostgreSql()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var userId = await AddUserAsync(db);
        var date = new DateOnly(2018, 11, 4);
        var schedule = new AgendamentoPresenca(
            "Snapshot timezone", "Observacao timezone", new TimeOnly(0, 30), new TimeOnly(2, 0),
            [DiaSemanaIso.Domingo], date.AddDays(-1), userId, new DateTimeOffset(2018, 11, 3, 12, 0, 0, TimeSpan.Zero));
        db.AgendamentosPresenca.Add(schedule);
        await db.SaveChangesAsync();
        var repository = new AgendamentoPresencaRepository(db);
        var now = new DateTimeOffset(2018, 11, 4, 5, 0, 0, TimeSpan.Zero);
        var version = db.Entry(schedule).Property<uint>("xmin").CurrentValue;

        var first = await repository.TryUpsertFailedTimeZoneOccurrenceAsync(
            schedule.Id, date, version, DiaSemanaIso.Domingo, schedule.HorarioPublicacaoLocal,
            schedule.HorarioEncerramentoLocal, now, CancellationToken.None);
        var second = await repository.TryUpsertFailedTimeZoneOccurrenceAsync(
            schedule.Id, date, version, DiaSemanaIso.Domingo, schedule.HorarioPublicacaoLocal,
            schedule.HorarioEncerramentoLocal, now.AddMinutes(1), CancellationToken.None);

        first.Should().Be(new AgendamentoPresencaOccurrenceWriteResult(
            OcorrenciaAgendamentoPresencaStatus.Falha, true));
        second.Should().Be(new AgendamentoPresencaOccurrenceWriteResult(
            OcorrenciaAgendamentoPresencaStatus.Falha, false));
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.CodigoFalha.Should().Be(MessageCodes.PresenceScheduleTimeZoneInvalid);
        occurrence.NomeSnapshot.Should().Be("Snapshot timezone");
        occurrence.ObservacaoSnapshot.Should().Be("Observacao timezone");
        occurrence.PublicacaoPrevistaEm.Should().Be(new DateTimeOffset(2018, 11, 4, 3, 30, 0, TimeSpan.Zero));
        occurrence.EncerramentoPrevistoEm.Should().Be(new DateTimeOffset(2018, 11, 4, 4, 0, 0, TimeSpan.Zero));
        (await db.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task FalhaTimezone_DeveRejeitarConfiguracaoStaleSemOcorrencia()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid scheduleId;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var schedule = new AgendamentoPresenca(
                "Timezone stale", null, new TimeOnly(0, 30), new TimeOnly(2, 0), [DiaSemanaIso.Domingo],
                new DateOnly(2018, 11, 3), userId, Agora);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            scheduleId = schedule.Id;
        }

        await using var staleContext = database.CreateContext();
        var staleRepository = new AgendamentoPresencaRepository(staleContext);
        var observed = await staleRepository.GetProcessingCandidateAsync(scheduleId, CancellationToken.None);
        await using (var editContext = database.CreateContext())
        {
            var edited = await new AgendamentoPresencaRepository(editContext)
                .GetByIdAsync(scheduleId, true, CancellationToken.None);
            edited!.Editar("Timezone corrigido", null, new TimeOnly(1, 0), new TimeOnly(2, 0),
                [DiaSemanaIso.Domingo], edited.CriadoPorUsuarioId, Agora.AddMinutes(1));
            await editContext.SaveChangesAsync();
        }

        var result = await staleRepository.TryUpsertFailedTimeZoneOccurrenceAsync(
            scheduleId, new DateOnly(2018, 11, 4), observed!.Version, DiaSemanaIso.Domingo,
            new TimeOnly(0, 30), new TimeOnly(2, 0), Agora.AddMinutes(2), CancellationToken.None);

        result.Changed.Should().BeFalse();
        await using var assertion = database.CreateContext();
        (await assertion.OcorrenciasAgendamentosPresenca.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConclusaoAposEsperarLockAteEncerramento_DeveRejeitarSemDraft()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        Guid occurrenceId;
        Guid claimId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var seed = database.CreateContext())
        {
            var userId = await AddUserAsync(seed);
            var localNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "America/Sao_Paulo");
            var date = DateOnly.FromDateTime(localNow.DateTime);
            var schedule = new AgendamentoPresenca(
                "Encerramento real", null, new TimeOnly(10, 0), new TimeOnly(11, 0),
                [(DiaSemanaIso)(localNow.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)localNow.DayOfWeek)],
                date.AddDays(-1), userId, now);
            seed.AgendamentosPresenca.Add(schedule);
            await seed.SaveChangesAsync();
            occurrenceId = Guid.NewGuid();
            await using var connection = await database.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO ocorrencias_agendamentos_presenca
                    (id, agendamento_presenca_id, data_local, publicacao_prevista_em, encerramento_previsto_em,
                     nome_snapshot, status, claim_id, claim_expires_at, ultima_tentativa_em, criada_em, atualizada_em)
                VALUES (@id, @scheduleId, @date, @publication, @closure,
                        'Encerramento real', 0, @claimId, @claimExpiresAt, @now, @now, @now)
                """;
            command.Parameters.AddWithValue("id", occurrenceId);
            command.Parameters.AddWithValue("scheduleId", schedule.Id);
            command.Parameters.AddWithValue("date", date);
            command.Parameters.AddWithValue("publication", now.AddMinutes(-1));
            command.Parameters.AddWithValue("closure", now.AddSeconds(2));
            command.Parameters.AddWithValue("claimId", claimId);
            command.Parameters.AddWithValue("claimExpiresAt", now.AddMinutes(5));
            command.Parameters.AddWithValue("now", now);
            await command.ExecuteNonQueryAsync();
        }

        await using var locker = await database.OpenConnectionAsync();
        await using var lockTransaction = await locker.BeginTransactionAsync();
        await using (var lockCommand = locker.CreateCommand())
        {
            lockCommand.Transaction = lockTransaction;
            lockCommand.CommandText = "SELECT id FROM ocorrencias_agendamentos_presenca WHERE id = @id FOR UPDATE";
            lockCommand.Parameters.AddWithValue("id", occurrenceId);
            await lockCommand.ExecuteScalarAsync();
        }

        await using var completionContext = database.CreateContext();
        var completion = new AgendamentoPresencaRepository(completionContext).TryCompleteWithDraftAsync(
            occurrenceId, claimId, CreateDraft(now.AddSeconds(2)), now, CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(3));
        await lockTransaction.CommitAsync();

        (await completion).Should().BeFalse();
        await using var assertion = database.CreateContext();
        (await assertion.DraftMontagens.AsNoTracking().CountAsync()).Should().Be(0);
        (await assertion.DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BloqueioDeveCongelarConfiguracaoENaoReescreverNoOp()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var userId = await AddUserAsync(db);
        var schedule = CreateSchedule(userId, "Nome original");
        db.AgendamentosPresenca.Add(schedule);
        await db.SaveChangesAsync();
        var repository = new AgendamentoPresencaRepository(db);
        var date = new DateOnly(2026, 7, 24);

        var first = await repository.TryUpsertBlockedOccurrenceAsync(
            schedule.Id, date, Agora, Agora.AddHours(2), MessageCodes.PresenceScheduleDiscordUnavailable,
            Agora, CancellationToken.None);
        db.ChangeTracker.Clear();
        var timestamp = (await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync()).AtualizadaEm;
        var second = await repository.TryUpsertBlockedOccurrenceAsync(
            schedule.Id, date, Agora, Agora.AddHours(2), MessageCodes.PresenceScheduleDiscordUnavailable,
            Agora.AddMinutes(1), CancellationToken.None);
        var afterNoOp = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        var tracked = await repository.GetByIdAsync(schedule.Id, true, CancellationToken.None);
        tracked!.Editar("Nome editado", "Observacao editada", tracked.HorarioPublicacaoLocal,
            tracked.HorarioEncerramentoLocal, tracked.DiasSemana.Select(item => item.DiaSemana).ToArray(),
            userId, Agora.AddMinutes(2));
        await repository.SaveChangesAsync(CancellationToken.None);
        var claimId = Guid.NewGuid();
        var claim = await repository.TryClaimOccurrenceAsync(
            schedule.Id, date, Agora, Agora.AddHours(2), claimId, Agora.AddMinutes(8),
            Agora.AddMinutes(3), CancellationToken.None);

        first.Changed.Should().BeTrue();
        second.Changed.Should().BeFalse();
        afterNoOp.AtualizadaEm.Should().Be(timestamp);
        claim.Should().NotBeNull();
        claim!.NomeSnapshot.Should().Be("Nome original");
        claim.ObservacaoSnapshot.Should().Be("Observacao");
        var occurrence = await db.OcorrenciasAgendamentosPresenca.AsNoTracking().SingleAsync();
        occurrence.NomeSnapshot.Should().Be("Nome original");
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
        new(name, "Observacao", new TimeOnly(15, 0), new TimeOnly(17, 0), days ?? [DiaSemanaIso.Sexta],
            new DateOnly(2026, 7, 23), userId, Agora);

    private static DraftMontagem CreateDraft(DateTimeOffset closure)
    {
        var draft = new DraftMontagem("Rinha agendada - 24/07/2026", "Observacao", 5,
            DraftMontagemCriterioCapitaes.Manual, [], []);
        draft.ConfigurarEncerramentoPresenca(closure);
        draft.ConfigurarPublicacaoDiscord("guild-1", null);
        return draft;
    }

    private static ProcessarAgendamentosPresencaDevidosCommandHandler CreateExecutionHandler(
        IAgendamentoPresencaRepository repository,
        DateTimeOffset? now = null,
        IDiscordConfigurationService? configurationService = null,
        AgendamentoPresencaProcessingOptions? options = null)
    {
        var discord = new Mock<IDiscordConfigurationService>();
        discord.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new DiscordConfigurationDto(
            Guid.NewGuid(), "guild-1", "presence-1", "news-1", "admin-1", "draft-1", "result-1", true));
        return new ProcessarAgendamentosPresencaDevidosCommandHandler(
            repository,
            new SaoPauloAgendamentoPresencaTimeZone(),
            configurationService ?? discord.Object,
            Mock.Of<IAgendamentoPresencaMetrics>(),
            Mock.Of<IAgendamentoPresencaDiagnostics>(),
            new IntegrationClock(now ?? Agora),
            options ?? new AgendamentoPresencaProcessingOptions());
    }

    private static async Task<bool> ExecuteOccurrenceUpsertAsync(
        AgendamentoPresencaRepository repository,
        string operation,
        Guid scheduleId,
        DateOnly date,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        DateTimeOffset now)
    {
        if (operation == "blocked")
        {
            return (await repository.TryUpsertBlockedOccurrenceAsync(
                scheduleId,
                date,
                publicationAt,
                closureAt,
                MessageCodes.PresenceScheduleDiscordUnavailable,
                now,
                CancellationToken.None)).Changed;
        }

        return (await repository.TryUpsertMissedOccurrenceAsync(
            scheduleId,
            date,
            publicationAt,
            closureAt,
            MessageCodes.PresenceScheduleWindowExpired,
            now,
            CancellationToken.None)).Changed;
    }

    private sealed record IntegrationClock(DateTimeOffset UtcNow) : ISystemClock;

    private sealed class BumpScheduleXminOnFirstSaveInterceptor(
        PostgreSqlTestDatabase database,
        Guid scheduleId) : SaveChangesInterceptor
    {
        private int invoked;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref invoked, 1) == 0)
            {
                await using var connection = await database.OpenConnectionAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "UPDATE agendamentos_presenca SET atualizado_em = atualizado_em + interval '1 microsecond' WHERE id = @id";
                command.Parameters.AddWithValue("id", scheduleId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            return result;
        }
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

    private static async Task InsertOccurrenceStateAsync(
        PostgreSqlTestDatabase database,
        Guid scheduleId,
        InvalidOccurrenceState state)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ocorrencias_agendamentos_presenca
                (id, agendamento_presenca_id, data_local, publicacao_prevista_em,
                 encerramento_previsto_em, nome_snapshot, status, draft_montagem_id, codigo_falha,
                 claim_id, claim_expires_at, ultima_tentativa_em, criada_em, atualizada_em)
            VALUES
                (@id, @scheduleId, @date, @publication, @closure, 'Snapshot', @status, @draftId, @code,
                 @claimId, @claimExpiresAt, @now, @now, @now)
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("scheduleId", scheduleId);
        command.Parameters.AddWithValue("date", new DateOnly(2026, 8, 1).AddDays(state.Status));
        command.Parameters.AddWithValue("publication", Agora);
        command.Parameters.AddWithValue("closure", Agora.AddHours(2));
        command.Parameters.AddWithValue("status", (short)state.Status);
        command.Parameters.AddWithValue("draftId", state.DraftId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("code", state.Status is 1 ? MessageCodes.PresenceScheduleDiscordUnavailable : DBNull.Value);
        command.Parameters.AddWithValue("claimId", state.ClaimId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("claimExpiresAt", state.ClaimExpiresAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("now", Agora);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteSqlAsync(PostgreSqlTestDatabase database, string sql)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed record InvalidOccurrenceState(
        short Status,
        Guid? ClaimId,
        DateTimeOffset? ClaimExpiresAt,
        Guid? DraftId);

    private static UpdateAgendamentoPresencaCommandHandler CreateUpdateHandler(IAgendamentoPresencaRepository repository) => new(
        repository,
        new AgendamentoPresencaRequestValidator(),
        new FixedClock(Agora));

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class ConcurrentSaveBarrierInterceptor(int expectedSaves) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _arrivals) == expectedSaves)
            {
                _release.TrySetResult();
            }

            await _release.Task.WaitAsync(cancellationToken);
            return result;
        }
    }

    private sealed class CommandCounterInterceptor : DbCommandInterceptor
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Reset() => Interlocked.Exchange(ref _count, 0);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }
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

        public RinhaDasLendasDbContext CreateContext(params IInterceptor[] interceptors)
        {
            var builder = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
                .UseNpgsql(ConnectionString);
            if (interceptors.Length > 0)
            {
                builder.AddInterceptors(interceptors);
            }

            var options = builder.Options;
            return new RinhaDasLendasDbContext(options);
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
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
