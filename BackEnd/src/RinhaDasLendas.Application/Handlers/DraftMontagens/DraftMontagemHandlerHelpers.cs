using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

internal static class DraftMontagemHandlerHelpers
{
    public static void EnsureActivePlayers(IReadOnlyCollection<Jogador> jogadores, IReadOnlyCollection<Guid> jogadoresIds)
    {
        if (jogadores.Count != jogadoresIds.Count)
        {
            throw new DomainException(MessageCodes.PlayerNotFound);
        }

        if (jogadores.Any(jogador => jogador.Status != JogadorStatus.Ativo))
        {
            throw new DomainException(MessageCodes.InactivePlayerCannotJoinQueue);
        }
    }

    public static IReadOnlyCollection<Guid> ResolveCapitaes(CreateDraftMontagemRequestDto request)
    {
        var quantidadeTimes = request.JogadoresIds.Count / request.TamanhoEquipe;
        if (!request.SortearCapitaes)
        {
            return request.CapitaesIds.ToList();
        }

        return request.JogadoresIds.OrderBy(_ => Guid.NewGuid()).Take(quantidadeTimes).ToList();
    }

    public static DraftMontagemLayoutTime ToDomain(DraftMontagemLayoutTimeDto time)
    {
        return new DraftMontagemLayoutTime(time.TimeId, time.Nome, time.CapitaoId, time.Jogadores.Select(ToDomain).ToList());
    }

    public static DraftMontagemLayoutParticipante ToDomain(DraftMontagemLayoutParticipanteDto participante)
    {
        var rota = string.IsNullOrWhiteSpace(participante.RotaContextual) ? (Rota?)null : Enum.Parse<Rota>(participante.RotaContextual, true);
        return new DraftMontagemLayoutParticipante(participante.JogadorId, participante.Ordem, rota);
    }
}
