using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class SalvarLayoutDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<SalvarLayoutDraftMontagemRequestDto> validator,
    IDraftMontagemRealtimePublisher publisher) : IRequestHandler<SalvarLayoutDraftMontagemCommand, DraftMontagemResponseDto?>
{
    public async Task<DraftMontagemResponseDto?> Handle(SalvarLayoutDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var montagem = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }
        if (montagem.VersaoEstado != command.Request.VersaoEstado)
        {
            throw new DomainException(MessageCodes.DraftStateConflict);
        }

        montagem.SalvarLayout(
            command.Request.Times.Select(DraftMontagemHandlerHelpers.ToDomain).ToList(),
            command.Request.Livres.Select(DraftMontagemHandlerHelpers.ToDomain).ToList(),
            command.Request.Reservas.Select(DraftMontagemHandlerHelpers.ToDomain).ToList());
        await repository.SaveChangesAsync(cancellationToken);
        await publisher.PublishAfterCommitAsync(command.Id);
        var updated = await repository.GetByIdAsync(command.Id, cancellationToken) ?? montagem;
        return DraftMontagemResponseDto.FromEntity(updated);
    }
}
