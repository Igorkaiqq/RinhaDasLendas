using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Drafts;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Drafts;

public sealed class CancelarDraftCommandHandler(
    IDraftRepository draftRepository,
    IValidator<CancelarDraftRequestDto> validator) : IRequestHandler<CancelarDraftCommand, DraftResponseDto?>
{
    public async Task<DraftResponseDto?> Handle(CancelarDraftCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var draft = await draftRepository.GetByIdAsync(command.DraftId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        draft.Cancelar(command.Request.Motivo);
        await draftRepository.SaveChangesAsync(cancellationToken);

        return DraftResponseDto.FromEntity(draft);
    }
}
