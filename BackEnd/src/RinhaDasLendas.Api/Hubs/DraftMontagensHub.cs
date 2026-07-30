using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Hubs;

[Authorize]
public sealed class DraftMontagensHub(ISender sender, IMessageProvider messages) : Hub
{
    public async Task JoinDraftMontagem(Guid draftMontagemId)
    {
        if (!await sender.Send(new CanViewDraftMontagemQuery(draftMontagemId), Context.ConnectionAborted))
        {
            throw new HubException(messages.GetMessage(MessageCodes.DraftRealtimeUnavailable));
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(draftMontagemId), Context.ConnectionAborted);
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
