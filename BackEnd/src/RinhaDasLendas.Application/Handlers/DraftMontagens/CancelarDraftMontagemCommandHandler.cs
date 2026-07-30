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
    IDraftMontagemRealtimePublisher publisher,
    IDraftMontagemMetrics metrics) : IRequestHandler<CancelarDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(CancelarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var currentUserId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.Cancelar(command.Request.Motivo, currentUserId);
        await repository.SaveChangesAsync(cancellationToken);
        metrics.RecordDraftCancelled(command.Id);
        await publisher.PublishAfterCommitAsync(command.Id);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
