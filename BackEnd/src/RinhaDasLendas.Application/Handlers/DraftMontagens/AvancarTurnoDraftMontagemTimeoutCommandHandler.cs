using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class AvancarTurnoDraftMontagemTimeoutCommandHandler(
    IDraftMontagemRepository repository,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<AvancarTurnoDraftMontagemTimeoutCommand, DraftMontagemRealtimeStateDto?>
{
    public async Task<DraftMontagemRealtimeStateDto?> Handle(AvancarTurnoDraftMontagemTimeoutCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (!montagem.AvancarTurnoPorTimeout(now))
        {
            return await DraftMontagemRealtimeStateFactory.CreateAsync(montagem, repository, currentUser, now, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        var state = await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, now, cancellationToken);
        await notifier.StateUpdatedAsync(command.Id, state, cancellationToken);
        return state;
    }
}
