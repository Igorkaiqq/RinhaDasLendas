using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class ReabrirPresencaDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<ReabrirPresencaDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(ReabrirPresencaDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var userId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.ReabrirPresenca(userId);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
