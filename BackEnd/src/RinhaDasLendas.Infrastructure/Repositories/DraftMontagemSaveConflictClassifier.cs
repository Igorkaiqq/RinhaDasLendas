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
        return exception switch
        {
            DbUpdateConcurrencyException => DraftMontagemSaveResultado.ConflitoDeVersao,
            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: PresenceByUserIndex or PresenceByPlayerIndex,
                },
            } => DraftMontagemSaveResultado.ConflitoDePresencaConfirmada,
            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: DiscordPublicationByTypeIndex,
                },
            } => DraftMontagemSaveResultado.ConflitoDeVersao,
            _ => null,
        };
    }

    public static bool IsStructuralUniqueViolation(Exception exception)
    {
        return exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ParticipantByPlayerIndex or TeamByOrderIndex,
            },
        };
    }
}
