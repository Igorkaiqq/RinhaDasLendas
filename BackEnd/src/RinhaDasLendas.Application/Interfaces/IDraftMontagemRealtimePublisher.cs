using RinhaDasLendas.Application.Enums;

namespace RinhaDasLendas.Application.Interfaces;

public interface IDraftMontagemRealtimePublisher
{
    Task PublishAfterCommitAsync(
        Guid draftId,
        DraftMontagemAvailabilityChange availability = DraftMontagemAvailabilityChange.None);
}
