using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;

namespace RinhaDasLendas.Domain.Repositories;

public interface IDraftMontagemRepository
{
    Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken);
    Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DraftMontagem?> ReloadByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DraftMontagem?> GetByIdIncludingArchivedAsync(Guid id, CancellationToken cancellationToken);
    Task<DraftMontagem?> ReloadByIdIncludingArchivedAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken);
    Task<Jogador?> GetJogadorByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> SearchJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, CancellationToken cancellationToken);
    Task<DraftMontagemPublicacaoClaim?> TryClaimPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora, CancellationToken cancellationToken);
    Task<bool> TryConcluirPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, string? guildId, string? channelId, string messageId, DateTimeOffset agora, CancellationToken cancellationToken);
    Task<bool> TryRegistrarFalhaPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, string? guildId, string? channelId, string? erroCodigo, DateTimeOffset agora, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> MarcarPublicacoesExpiradasParaReconciliacaoAsync(DateTimeOffset agora, CancellationToken cancellationToken);
    Task<DraftMontagemSaveResultado> TrySaveChangesAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
