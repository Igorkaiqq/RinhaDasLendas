using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Tests.Fixtures;

namespace RinhaDasLendas.Tests.Integration;

public sealed class CompetitiveRepositoryTests
{
    [Fact]
    public async Task CalendarRepository_ShouldUseTrackedCommandLoadsAndEfficientReadQueries()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var data = await SeedAsync(database);
        await using var context = database.CreateContext();
        var repository = new CalendarioCompetitivoRepository(context);

        var lightweightCalendar = await repository.GetCalendarAsync(CancellationToken.None);
        lightweightCalendar.Should().NotBeNull();
        context.Entry(lightweightCalendar!).State.Should().Be(EntityState.Detached);

        var activeSeason = await repository.GetActiveSeasonAsync(CancellationToken.None);
        activeSeason!.Id.Should().Be(data.ActiveSeasonId);
        context.Entry(activeSeason).State.Should().Be(EntityState.Detached);

        var page = await repository.ListSeasonsAsync(
            [data.ActiveSeasonId], SeasonEstado.Ativa, 1, 10, CancellationToken.None);
        page.Should().ContainSingle().Which.Id.Should().Be(data.ActiveSeasonId);
        context.Entry(page.Single()).State.Should().Be(EntityState.Detached);
        (await repository.CountSeasonsAsync(
            [data.ActiveSeasonId], SeasonEstado.Ativa, CancellationToken.None)).Should().Be(1);

        (await repository.ExistsOverlappingSeasonAsync(
            new DateOnly(2026, 12, 31), new DateOnly(2027, 2, 1), null, CancellationToken.None)).Should().BeTrue();
        (await repository.ExistsOverlappingSeasonAsync(
            new DateOnly(2027, 1, 1), new DateOnly(2028, 1, 1), data.ActiveSeasonId, CancellationToken.None)).Should().BeFalse();
        (await repository.ExistsSeasonOrderAsync(2026, 1, null, CancellationToken.None)).Should().BeTrue();
        (await repository.ExistsSeasonOrderAsync(2026, 1, data.ActiveSeasonId, CancellationToken.None)).Should().BeFalse();

        context.ChangeTracker.Clear();
        var commandCalendar = await repository.GetWithSeasonsAsync(CancellationToken.None);
        var commandSeason = await repository.GetSeasonByIdAsync(data.ActiveSeasonId, CancellationToken.None);
        context.Entry(commandCalendar!).State.Should().Be(EntityState.Unchanged);
        context.Entry(commandSeason!).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task CompetitionRepository_ShouldLoadOrderedChildrenFilterUniquenessAndAvoidSaving()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var data = await SeedAsync(database);
        var saveObserver = new SaveObserver();
        await using var context = database.CreateContext(saveObserver);
        var repository = new CompeticaoRepository(context);

        var competition = await repository.GetWithRoundsAndRulesAsync(data.DailyCompetitionId, CancellationToken.None);
        competition.Should().NotBeNull();
        competition!.Rodadas.Select(item => item.Ordem).Should().Equal(1, 2);
        competition.VersoesRegras.Select(item => item.Numero).Should().Equal(1, 2);
        context.Entry(competition).State.Should().Be(EntityState.Unchanged);
        competition.Rodadas.Should().OnlyContain(item => context.Entry(item).State == EntityState.Unchanged);

        context.ChangeTracker.Clear();
        var page = await repository.ListAsync([data.ActiveSeasonId], 1, 1, CancellationToken.None);
        page.Should().ContainSingle();
        page.Single().Rodadas.Should().NotBeEmpty();
        context.Entry(page.Single()).State.Should().Be(EntityState.Detached);
        (await repository.CountAsync([data.ActiveSeasonId], CancellationToken.None)).Should().Be(2);
        (await repository.ExistsCodeAsync(data.ActiveSeasonId, "daily", null, CancellationToken.None)).Should().BeTrue();
        (await repository.ExistsCodeAsync(
            data.ActiveSeasonId, "DAILY", data.DailyCompetitionId, CancellationToken.None)).Should().BeFalse();
        (await repository.ExistsDailyCircuitAsync(data.ActiveSeasonId, null, CancellationToken.None)).Should().BeTrue();
        (await repository.ExistsDailyCircuitAsync(
            data.ActiveSeasonId, data.DailyCompetitionId, CancellationToken.None)).Should().BeFalse();
        (await repository.GetNextGeneralRulesNumberAsync(data.ActiveSeasonId, CancellationToken.None)).Should().Be(2);

        var rules = await repository.GetRulesVersionAsync(data.CompetitionRulesId, CancellationToken.None);
        rules.Should().NotBeNull();
        context.Entry(rules!).State.Should().Be(EntityState.Detached);

        var newCompetition = new Competicao(
            data.ActiveSeasonId, "Nova", "NOVA", false, data.ActorId, DateTimeOffset.UtcNow);
        var newRound = new Rodada(newCompetition.Id, "Rodada", 1, DateTimeOffset.UtcNow);
        var newRules = new VersaoRegras(
            data.ActiveSeasonId, newCompetition.Id, 1, SerieFormato.Md3, ModoDraft.Padrao,
            data.ActorId, DateTimeOffset.UtcNow);
        await repository.AddAsync(newCompetition, CancellationToken.None);
        await repository.AddRoundAsync(newRound, CancellationToken.None);
        await repository.AddRulesVersionAsync(newRules, CancellationToken.None);

        saveObserver.SaveCalls.Should().Be(0);
        context.Entry(newCompetition).State.Should().Be(EntityState.Added);
        context.Entry(newRound).State.Should().Be(EntityState.Added);
        context.Entry(newRules).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task SerieRepository_ShouldTranslateConfirmedPeriodAndStartedScopeChecksToPostgres()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var data = await SeedAsync(database);
        await using var context = database.CreateContext();
        var repository = new SerieRepository(context);

        (await repository.HasConfirmedSeriesOutsidePeriodAsync(
            data.ActiveSeasonId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            CancellationToken.None)).Should().BeTrue();
        (await repository.HasConfirmedSeriesOutsidePeriodAsync(
            data.ActiveSeasonId,
            new DateOnly(2025, 1, 1),
            new DateOnly(2027, 1, 1),
            CancellationToken.None)).Should().BeFalse();
        (await repository.HasStartedSeriesAsync(
            data.ActiveSeasonId, data.DailyCompetitionId, CancellationToken.None)).Should().BeTrue();
        (await repository.HasStartedSeriesAsync(
            data.ActiveSeasonId, data.OtherCompetitionId, CancellationToken.None)).Should().BeFalse();
        (await repository.ListCompetitionIdsWithStartedSeriesAsync(
            [data.DailyCompetitionId, data.OtherCompetitionId], CancellationToken.None))
            .Should().BeEquivalentTo([data.DailyCompetitionId]);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Theory]
    [InlineData("Rascunho")]
    [InlineData("Confirmada")]
    [InlineData("Remake")]
    [InlineData("Anulada")]
    public async Task SerieRepository_ShouldTreatCanceledSeriesWithMatchEvidenceAsStarted(string matchState)
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var data = await SeedAsync(database);
        var competitionId = await InsertCanceledSeriesScopeAsync(database, data, matchState);
        await using var context = database.CreateContext();
        var repository = new SerieRepository(context);

        (await repository.HasStartedSeriesAsync(
            data.ActiveSeasonId, competitionId, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task SerieRepository_ShouldNotTreatCanceledBeforeStartAsStarted()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var data = await SeedAsync(database);
        var competitionId = await InsertCanceledSeriesScopeAsync(database, data, matchState: null);
        await using var context = database.CreateContext();
        var repository = new SerieRepository(context);

        (await repository.HasStartedSeriesAsync(
            data.ActiveSeasonId, competitionId, CancellationToken.None)).Should().BeFalse();
    }

    private static async Task<SeedData> SeedAsync(CompetitivePostgresFixture database)
    {
        var data = new SeedData(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO usuarios
                (id, nome, ativo, data_cadastro, data_atualizacao, email_confirmed, phone_number_confirmed,
                 two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES (@actor_id, 'Ator', TRUE, NOW(), NOW(), FALSE, FALSE, FALSE, FALSE, 0);

            INSERT INTO seasons
                (id, nome, ano, ordem_no_ano, data_inicio, data_fim_exclusiva, estado, versao, criada_em,
                 atualizada_em, ativada_em, criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@old_season_id, 'Anterior', 2025, 1, DATE '2025-01-01', DATE '2026-01-01', 'Encerrada', 1,
                 NOW(), NOW(), NOW(), @actor_id, @actor_id),
                (@active_season_id, 'Atual', 2026, 1, DATE '2026-01-01', DATE '2027-01-01', 'Ativa', 1,
                 NOW(), NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO calendarios_competitivos
                (id, season_ativa_id, versao, atualizado_em, atualizado_por_usuario_id)
            VALUES (@calendar_id, @active_season_id, 1, NOW(), @actor_id);

            INSERT INTO competicoes
                (id, season_id, nome, codigo, circuito_diario, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@daily_competition_id, @active_season_id, 'Diária', 'DAILY', TRUE, 0, NOW(), NOW(), @actor_id, @actor_id),
                (@other_competition_id, @active_season_id, 'Outra', 'OTHER', FALSE, 0, NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO rodadas (id, competicao_id, nome, ordem, versao, criada_em, atualizada_em)
            VALUES
                (gen_random_uuid(), @daily_competition_id, 'Segunda', 2, 0, NOW(), NOW()),
                (gen_random_uuid(), @daily_competition_id, 'Primeira', 1, 0, NOW(), NOW());

            INSERT INTO versoes_regras
                (id, season_id, competicao_id, numero, formato, modo_draft, publicada_em, publicada_por_usuario_id)
            VALUES
                (gen_random_uuid(), @active_season_id, NULL, 1, 'Md3', 'Padrao', NOW(), @actor_id),
                (gen_random_uuid(), @active_season_id, @daily_competition_id, 2, 'Md5', 'Fearless', NOW(), @actor_id),
                (@competition_rules_id, @active_season_id, @daily_competition_id, 1, 'Md3', 'Padrao', NOW(), @actor_id),
                (gen_random_uuid(), @active_season_id, @other_competition_id, 1, 'Md3', 'Padrao', NOW(), @actor_id);

            INSERT INTO series
                (id, season_id, competicao_id, versao_regras_id, tipo, formato, modo_draft, fearless_habilitado,
                 estado, agendada_para, revisao_necessaria, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@started_series_id, @active_season_id, @daily_competition_id, @competition_rules_id,
                 'Amistoso', 'Md3', 'Padrao', FALSE, 'EmAndamento', TIMESTAMPTZ '2025-12-31 12:00:00+00',
                 FALSE, 0, NOW(), NOW(), @actor_id, @actor_id),
                (gen_random_uuid(), @active_season_id, @other_competition_id,
                 (SELECT id FROM versoes_regras WHERE competicao_id = @other_competition_id),
                 'Amistoso', 'Md3', 'Padrao', FALSE, 'Agendada', TIMESTAMPTZ '2026-06-01 12:00:00+00',
                 FALSE, 0, NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO lados_series (id, serie_id, ordem, tipo, origem_id, nome_snapshot)
            VALUES
                (@winner_side_id, @started_series_id, 1, 'TimeOficial', gen_random_uuid(), 'A'),
                (gen_random_uuid(), @started_series_id, 2, 'TimeOficial', gen_random_uuid(), 'B');

            INSERT INTO partidas
                (id, serie_id, ordem, estado, lado_vencedor_id, motivo_termino, conflito_fearless,
                 versao, criada_em, atualizada_em, confirmada_em)
            VALUES
                (gen_random_uuid(), @started_series_id, 1, 'Confirmada', @winner_side_id, 'Normal', FALSE,
                 0, NOW(), NOW(), NOW());
            """;
        command.Parameters.AddWithValue("actor_id", data.ActorId);
        command.Parameters.AddWithValue("calendar_id", data.CalendarId);
        command.Parameters.AddWithValue("old_season_id", Guid.NewGuid());
        command.Parameters.AddWithValue("active_season_id", data.ActiveSeasonId);
        command.Parameters.AddWithValue("daily_competition_id", data.DailyCompetitionId);
        command.Parameters.AddWithValue("other_competition_id", data.OtherCompetitionId);
        command.Parameters.AddWithValue("competition_rules_id", data.CompetitionRulesId);
        command.Parameters.AddWithValue("started_series_id", Guid.NewGuid());
        command.Parameters.AddWithValue("winner_side_id", Guid.NewGuid());
        await command.ExecuteNonQueryAsync();
        return data;
    }

    private static async Task<Guid> InsertCanceledSeriesScopeAsync(
        CompetitivePostgresFixture database,
        SeedData data,
        string? matchState)
    {
        var competitionId = Guid.NewGuid();
        var rulesId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO competicoes
                (id, season_id, nome, codigo, circuito_diario, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES (@competition_id, @season_id, 'Cancelada', @code, FALSE, 0, NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO versoes_regras
                (id, season_id, competicao_id, numero, formato, modo_draft, publicada_em, publicada_por_usuario_id)
            VALUES (@rules_id, @season_id, @competition_id, 1, 'Md3', 'Padrao', NOW(), @actor_id);

            INSERT INTO series
                (id, season_id, competicao_id, versao_regras_id, tipo, formato, modo_draft, fearless_habilitado,
                 estado, agendada_para, revisao_necessaria, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES (@series_id, @season_id, @competition_id, @rules_id, 'Amistoso', 'Md3', 'Padrao', FALSE,
                 'Cancelada', TIMESTAMPTZ '2026-06-01 12:00:00+00', FALSE, 0, NOW(), NOW(), @actor_id, @actor_id);
            """;
        if (matchState is not null)
        {
            command.CommandText +=
                """

                INSERT INTO partidas
                    (id, serie_id, ordem, estado, conflito_fearless, versao, criada_em, atualizada_em)
                VALUES (gen_random_uuid(), @series_id, 1, @match_state, FALSE, 0, NOW(), NOW());
                """;
            command.Parameters.AddWithValue("match_state", matchState);
        }

        command.Parameters.AddWithValue("competition_id", competitionId);
        command.Parameters.AddWithValue("season_id", data.ActiveSeasonId);
        command.Parameters.AddWithValue("code", $"C{competitionId:N}");
        command.Parameters.AddWithValue("actor_id", data.ActorId);
        command.Parameters.AddWithValue("rules_id", rulesId);
        command.Parameters.AddWithValue("series_id", seriesId);
        await command.ExecuteNonQueryAsync();
        return competitionId;
    }

    private sealed record SeedData(
        Guid ActorId,
        Guid CalendarId,
        Guid ActiveSeasonId,
        Guid DailyCompetitionId,
        Guid OtherCompetitionId,
        Guid CompetitionRulesId);

    private sealed class SaveObserver : SaveChangesInterceptor
    {
        public int SaveCalls { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return ValueTask.FromResult(result);
        }
    }
}
