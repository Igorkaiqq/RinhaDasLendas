using Microsoft.EntityFrameworkCore;
using Npgsql;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Infrastructure.Persistence;

internal static class CompetitiveSaveConflictClassifier
{
    public static string? Classify(Exception exception) => exception switch
    {
        DbUpdateException { InnerException: { } innerException } => Classify(innerException),
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_calendarios_competitivos_singleton"
        } => MessageCodes.CompetitiveResourceVersionStale,
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_seasons_ativa"
        } => MessageCodes.ActiveSeasonConflict,
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_seasons_ano_ordem_no_ano"
        } => MessageCodes.SeasonOrderConflict,
        PostgresException
        {
            SqlState: PostgresErrorCodes.ExclusionViolation,
            ConstraintName: "ex_seasons_periodo"
        } => MessageCodes.SeasonPeriodOverlap,
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_competicoes_season_id_codigo"
        } => MessageCodes.CompetitionCodeConflict,
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_competicoes_circuito_diario"
        } => MessageCodes.DailyCircuitCompetitionInvalid,
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_rodadas_competicao_id_ordem"
        } => MessageCodes.RoundOrderConflict,
        PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_versoes_regras_season_numero_geral"
                or "ux_versoes_regras_competicao_numero"
        } => MessageCodes.CompetitiveResourceVersionStale,
        _ => null
    };
}
