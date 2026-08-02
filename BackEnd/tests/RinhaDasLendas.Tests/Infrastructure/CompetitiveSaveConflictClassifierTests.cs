using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Infrastructure;

public sealed class CompetitiveSaveConflictClassifierTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_competicoes_season_id_codigo", MessageCodes.CompetitionCodeConflict)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_competicoes_circuito_diario", MessageCodes.DailyCircuitCompetitionInvalid)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_rodadas_competicao_id_ordem", MessageCodes.RoundOrderConflict)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_versoes_regras_season_numero_geral", MessageCodes.CompetitiveResourceVersionStale)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_versoes_regras_competicao_numero", MessageCodes.CompetitiveResourceVersionStale)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_seasons_ativa", MessageCodes.ActiveSeasonConflict)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_seasons_ano_ordem_no_ano", MessageCodes.SeasonOrderConflict)]
    [InlineData(PostgresErrorCodes.ExclusionViolation, "ex_seasons_periodo", MessageCodes.SeasonPeriodOverlap)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_calendarios_competitivos_singleton", MessageCodes.CompetitiveResourceVersionStale)]
    public void DeveClassificarSomenteConflitosCompetitivosConhecidos(
        string sqlState,
        string constraintName,
        string expectedMessageCode)
    {
        CompetitiveSaveConflictClassifier.Classify(WrappedPostgresException(sqlState, constraintName))
            .Should().Be(expectedMessageCode);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_seasons_ativa", MessageCodes.ActiveSeasonConflict)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ux_seasons_ano_ordem_no_ano", MessageCodes.SeasonOrderConflict)]
    [InlineData(PostgresErrorCodes.ExclusionViolation, "ex_seasons_periodo", MessageCodes.SeasonPeriodOverlap)]
    public void DeveClassificarConflitosDeSeasonLancadosDiretamentePeloPostgres(
        string sqlState,
        string constraintName,
        string expectedMessageCode)
    {
        CompetitiveSaveConflictClassifier.Classify(PostgresFailure(sqlState, constraintName))
            .Should().Be(expectedMessageCode);
    }

    [Fact]
    public void NaoDeveClassificarConstraintDesconhecida()
    {
        CompetitiveSaveConflictClassifier.Classify(WrappedPostgresException(
                PostgresErrorCodes.UniqueViolation,
                "ux_usuarios_normalized_user_name"))
            .Should().BeNull();
    }

    [Fact]
    public void DeveClassificarPostgresExceptionDiretaDoCommitDiferido()
    {
        CompetitiveSaveConflictClassifier.Classify(PostgresUniqueViolation("ux_rodadas_competicao_id_ordem"))
            .Should().Be(MessageCodes.RoundOrderConflict);
    }

    [Fact]
    public async Task UnitOfWork_DeveTraduzirConflitoConhecidoAntesDaBordaHttp()
    {
        var exception = WrappedPostgresException(
            PostgresErrorCodes.UniqueViolation,
            "ux_competicoes_season_id_codigo");
        var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .AddInterceptors(new ThrowingSaveInterceptor(exception))
            .Options;
        await using var context = new RinhaDasLendasDbContext(options);
        var unitOfWork = new CompetitiveUnitOfWork(context);

        var action = () => unitOfWork.SaveChangesAsync(CancellationToken.None);

        var translated = (await action.Should().ThrowAsync<DomainException>()).Which;
        translated.MessageCode.Should().Be(MessageCodes.CompetitionCodeConflict);
    }

    private static DbUpdateException WrappedPostgresException(string sqlState, string constraintName) =>
        new("save failed", PostgresFailure(sqlState, constraintName));

    private static PostgresException PostgresUniqueViolation(string constraintName) =>
        PostgresFailure(PostgresErrorCodes.UniqueViolation, constraintName);

    private static PostgresException PostgresFailure(string sqlState, string constraintName) =>
        new(
            "constraint conflict",
            "ERROR",
            "ERROR",
            sqlState,
            constraintName: constraintName);

    private sealed class ThrowingSaveInterceptor(DbUpdateException exception) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(exception);
    }
}
