using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

internal static class DraftMontagemRealtimeStateFactory
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
        return new DraftMontagemRealtimeStateDto(DraftMontagemResponseDto.FromEntity(montagem), now, canPick);
    }
}
