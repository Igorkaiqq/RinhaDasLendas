using Microsoft.AspNetCore.SignalR;
using RinhaDasLendas.Api.Hubs;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemRealtimeNotifier(IHubContext<DraftMontagensHub> hubContext) : IDraftMontagemRealtimeNotifier
{
    public Task SharedStateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeSnapshotDto state, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(DraftMontagensHub.GroupName(draftMontagemId)).SendAsync("DraftMontagemStateUpdated", state, cancellationToken);
    }

    public Task ArchivedAsync(Guid draftMontagemId, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.SendAsync("DraftMontagemArchived", draftMontagemId, cancellationToken);
    }

    public Task RestoredAsync(Guid draftMontagemId, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.SendAsync("DraftMontagemRestored", draftMontagemId, cancellationToken);
    }
}
