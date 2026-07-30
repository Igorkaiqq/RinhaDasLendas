using Microsoft.EntityFrameworkCore;
using Npgsql;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Infrastructure.Repositories;

internal static class DraftMontagemSaveConflictClassifier
{
    private const string PresenceByUserIndex = "ix_draft_montagem_presencas_draft_montagem_id_usuario_id";
    private const string PresenceByPlayerIndex = "ix_draft_montagem_presencas_draft_montagem_id_jogador_id";
    private const string DiscordPublicationByTypeIndex = "IX_draft_montagem_publicacoes_discord_draft_montagem_id_tipo";
    private const string ParticipantByPlayerIndex = "IX_draft_montagem_participantes_draft_montagem_id_jogador_id";
    private const string TeamByOrderIndex = "IX_draft_montagem_times_draft_montagem_id_ordem";

    public static DraftMontagemSaveResultado? Classify(Exception exception)
    {
        if (FindException<DbUpdateConcurrencyException>(exception) is not null)
        {
            return DraftMontagemSaveResultado.ConflitoDeVersao;
        }

        var postgresException = FindException<PostgresException>(exception);
        if (postgresException?.SqlState == PostgresErrorCodes.DeadlockDetected)
        {
            return DraftMontagemSaveResultado.ConflitoDeVersao;
        }

        if (postgresException?.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return null;
        }

        return postgresException.ConstraintName switch
        {
            PresenceByUserIndex or PresenceByPlayerIndex => DraftMontagemSaveResultado.ConflitoDePresencaConfirmada,
            DiscordPublicationByTypeIndex => DraftMontagemSaveResultado.ConflitoDeVersao,
            _ => null,
        };
    }

    public static bool IsStructuralUniqueViolation(Exception exception)
    {
        var postgresException = FindException<PostgresException>(exception);
        return postgresException is
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ParticipantByPlayerIndex or TeamByOrderIndex,
        };
    }

    private static TException? FindException<TException>(Exception? exception)
        where TException : Exception
    {
        while (exception is not null)
        {
            if (exception is TException matchingException)
            {
                return matchingException;
            }

            exception = exception.InnerException;
        }

        return null;
    }
}
