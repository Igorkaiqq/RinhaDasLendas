using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class CancelarDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<CancelarDraftMontagemRequestDto> validator,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier) : IRequestHandler<CancelarDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(CancelarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.Cancelar(command.Request.Motivo);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
