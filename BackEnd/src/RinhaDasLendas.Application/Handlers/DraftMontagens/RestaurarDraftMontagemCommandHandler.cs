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

public sealed class RestaurarDraftMontagemCommandHandler(
    IDraftMontagemRepository repository,
    IValidator<RestaurarDraftMontagemRequestDto> validator,
    ICurrentUser currentUser)
    : IRequestHandler<RestaurarDraftMontagemCommand, DraftMontagemArquivamentoResultadoDto?>
{
    public async Task<DraftMontagemArquivamentoResultadoDto?> Handle(RestaurarDraftMontagemCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var userId = DraftMontagemHandlerHelpers.ResolveRequiredCurrentUserId(currentUser);
        var montagem = await repository.GetByIdIncludingArchivedAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return null;
        }
        if (!montagem.Arquivado)
        {
            return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
        }
        if (montagem.VersaoEstado != command.Request.VersaoEstado)
        {
            throw new DomainException(MessageCodes.DraftStateConflict);
        }

        montagem.Restaurar(userId, DateTimeOffset.UtcNow);
        if (await repository.TrySaveChangesAsync(cancellationToken) != DraftMontagemSaveResultado.Persistido)
        {
            var current = await repository.ReloadByIdIncludingArchivedAsync(command.Id, cancellationToken);
            if (current is not null && !current.Arquivado)
            {
                return DraftMontagemArquivamentoResultadoDto.FromEntity(current);
            }
            throw new DomainException(MessageCodes.DraftStateConflict);
        }
        return DraftMontagemArquivamentoResultadoDto.FromEntity(montagem);
    }
}
