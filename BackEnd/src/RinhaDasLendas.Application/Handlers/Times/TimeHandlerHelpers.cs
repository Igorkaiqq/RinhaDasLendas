using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Times;

internal static class TimeHandlerHelpers
{
    public static async Task EnsureUniqueActiveNameAndTagAsync(
        ITimeRepository repository,
        string nome,
        string tag,
        Guid? ignoredId,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsActiveNameAsync(Time.NormalizarChave(nome), ignoredId, cancellationToken))
        {
            throw new DomainException(MessageCodes.TeamAlreadyExists);
        }

        if (await repository.ExistsActiveTagAsync(Time.NormalizarChave(tag), ignoredId, cancellationToken))
        {
            throw new DomainException(MessageCodes.TeamAlreadyExists);
        }
    }

    public static async Task EnsureActivePlayersAsync(
        ITimeRepository repository,
        IReadOnlyCollection<Guid> jogadoresIds,
        CancellationToken cancellationToken)
    {
        var jogadores = await repository.GetJogadoresByIdsAsync(jogadoresIds, cancellationToken);

        if (jogadores.Count != jogadoresIds.Distinct().Count())
        {
            throw new DomainException(MessageCodes.PlayerNotFound);
        }

        if (jogadores.Any(jogador => jogador.Status != JogadorStatus.Ativo))
        {
            throw new DomainException(MessageCodes.InactivePlayerCannotJoinTeam);
        }
    }
}
