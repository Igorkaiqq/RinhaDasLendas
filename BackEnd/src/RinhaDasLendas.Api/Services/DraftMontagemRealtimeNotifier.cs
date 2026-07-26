using Microsoft.AspNetCore.SignalR;
using RinhaDasLendas.Api.Hubs;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemRealtimeNotifier(IHubContext<DraftMontagensHub> hubContext) : IDraftMontagemRealtimeNotifier
{
    public Task StateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeStateDto state, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(DraftMontagensHub.GroupName(draftMontagemId)).SendAsync("DraftMontagemStateUpdated", state, cancellationToken);
    }

    public Task ArchivedAsync(Guid draftMontagemId, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.SendAsync("DraftMontagemArchived", draftMontagemId, cancellationToken);
    }
}
