using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Application.Handlers.Drafts;

internal static class DraftHandlerHelpers
{
    public static void EnsureActivePlayers(IReadOnlyCollection<Jogador> jogadores, IReadOnlyCollection<Guid> jogadoresIds)
    {
        var encontrados = jogadores.Select(jogador => jogador.Id).ToHashSet();
        if (jogadoresIds.Any(id => !encontrados.Contains(id)))
        {
            throw new DomainException(MessageCodes.DraftInvalidPlayer);
        }

        if (jogadores.Any(jogador => jogador.Status != JogadorStatus.Ativo))
        {
            throw new DomainException(MessageCodes.InactivePlayerCannotJoinQueue);
        }
    }

    public static (Guid CapitaoA, Guid CapitaoB) ResolveCaptainIds(CreateDraftRequestDtoAdapter request)
    {
        if (!request.SortearCapitaes)
        {
            return (request.CapitaoTimeAId!.Value, request.CapitaoTimeBId!.Value);
        }

        var shuffled = request.JogadoresIds.OrderBy(_ => Guid.NewGuid()).Take(2).ToList();
        if (shuffled.Count < 2)
        {
            throw new DomainException(MessageCodes.DraftPlayersRequired);
        }

        return (shuffled[0], shuffled[1]);
    }

    public static DraftTime ResolveFirstPick(bool sortearPrimeiroPick, string? primeiroTime)
    {
        if (sortearPrimeiroPick)
        {
            return Random.Shared.Next(0, 2) == 0 ? DraftTime.TimeA : DraftTime.TimeB;
        }

        return Enum.TryParse<DraftTime>(primeiroTime, ignoreCase: true, out var parsed) ? parsed : DraftTime.TimeA;
    }
}

internal sealed record CreateDraftRequestDtoAdapter(
    bool SortearCapitaes,
    Guid? CapitaoTimeAId,
    Guid? CapitaoTimeBId,
    IReadOnlyCollection<Guid> JogadoresIds);
