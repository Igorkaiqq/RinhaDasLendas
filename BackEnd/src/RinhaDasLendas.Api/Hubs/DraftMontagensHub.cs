using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RinhaDasLendas.Api.Hubs;

[Authorize]
public sealed class DraftMontagensHub : Hub
{
    public Task JoinDraftMontagem(Guid draftMontagemId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(draftMontagemId));
    }

    public Task LeaveDraftMontagem(Guid draftMontagemId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(draftMontagemId));
    }

    public static string GroupName(Guid draftMontagemId)
    {
        return $"draft-montagem:{draftMontagemId}";
    }
}
