using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public static class DraftMontagemRealtimeStateFactory
{
    public static async Task<DraftMontagemRealtimeStateDto> CreateAsync(
        DraftMontagem montagem,
        IDraftMontagemRepository repository,
        ICurrentUser currentUser,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var jogador = currentUser.UserId is Guid userId
            ? await repository.GetJogadorByUsuarioIdAsync(userId, cancellationToken)
            : null;

        var canPick = jogador is not null && montagem.TurnoAtualCapitaoId == jogador.Id;
        return Create(montagem, now, canPick);
    }

    public static DraftMontagemRealtimeStateDto Create(DraftMontagem montagem, DateTimeOffset now, bool canCurrentUserPick = false)
    {
        return new DraftMontagemRealtimeStateDto(DraftMontagemPublicResponseDto.FromEntity(montagem), now, canCurrentUserPick);
    }
}
