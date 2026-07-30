using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class ArquivarDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<ArquivarDraftMontagemRequestDto> validator,
    ICurrentUser currentUser,
    IDraftMontagemRealtimePublisher publisher)
    : IRequestHandler<ArquivarDraftMontagemCommand, DraftMontagemArquivamentoResultadoDto?>
{
    public async Task<DraftMontagemArquivamentoResultadoDto?> Handle(ArquivarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var userId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdIncludingArchivedAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }
        if (montagem.Arquivado)
        {
            return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
        }
        if (montagem.VersaoEstado != command.Request.VersaoEstado)
        {
            throw new DomainException(MessageCodes.DraftStateConflict);
        }

        montagem.Arquivar(command.Request.Motivo, userId, DateTimeOffset.UtcNow);
        if (await repository.TrySaveChangesAsync(cancellationToken) != DraftMontagemSaveResultado.Persistido)
        {
            var current = await repository.ReloadByIdIncludingArchivedAsync(command.Id, cancellationToken);
            if (current?.Arquivado == true)
            {
                return DraftMontagemArquivamentoResultadoDto.FromEntity(current);
            }
            throw new DomainException(MessageCodes.DraftStateConflict);
        }

        await publisher.PublishAfterCommitAsync(
            command.Id,
            DraftMontagemSnapshotScope.IncludingArchived,
            DraftMontagemAvailabilityChange.Archived);
        return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
    }
}
