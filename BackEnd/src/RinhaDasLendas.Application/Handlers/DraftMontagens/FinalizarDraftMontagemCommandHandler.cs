using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class FinalizarDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IDraftMontagemRealtimePublisher publisher) : IRequestHandler<FinalizarDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(FinalizarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }

        montagem.Finalizar();
        if (await repository.TrySaveChangesAsync(cancellationToken) != DraftMontagemSaveResultado.Persistido)
        {
            throw new DomainException(MessageCodes.DraftStateConflict);
        }
        await publisher.PublishAfterCommitAsync(command.Id);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
