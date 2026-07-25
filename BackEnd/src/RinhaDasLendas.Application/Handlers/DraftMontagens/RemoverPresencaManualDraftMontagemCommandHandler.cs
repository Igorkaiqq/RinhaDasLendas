using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class RemoverPresencaManualDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<RemoverPresencaManualDraftMontagemRequestDto> validator,
    ICurrentUser currentUser,
    IDraftMontagemRealtimeNotifier notifier,
    IDraftMontagemMetrics metrics) : IRequestHandler<RemoverPresencaManualDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(RemoverPresencaManualDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var currentUserId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.RemoverPresencaManual(command.Request.JogadorId, currentUserId, command.Request.Motivo);
        await repository.SaveChangesAsync(cancellationToken);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        await notifier.StateUpdatedAsync(command.Id, await DraftMontagemRealtimeStateFactory.CreateAsync(updated, repository, currentUser, DateTimeOffset.UtcNow, cancellationToken), cancellationToken);
        metrics.RecordPresenceCancelled(command.Id, DraftMontagemPresencaOrigem.Manual.ToString());
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
