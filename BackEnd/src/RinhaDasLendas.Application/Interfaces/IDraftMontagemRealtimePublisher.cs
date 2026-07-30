using RinhaDasLendas.Application.Enums;

namespace RinhaDasLendas.Application.Interfaces;

public interface IDraftMontagemRealtimePublisher
{
    Task PublishAfterCommitAsync(
        Guid draftId,
        DraftMontagemSnapshotScope snapshotScope = DraftMontagemSnapshotScope.Active,
        DraftMontagemAvailabilityChange availability = DraftMontagemAvailabilityChange.None);
}
