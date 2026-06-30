using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class FinalizarDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<FinalizarDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(FinalizarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.Finalizar();
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
