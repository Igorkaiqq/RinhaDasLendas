using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.Drafts;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.Drafts;

public sealed class RegistrarPickDraftCommandHandler(
    IDraftRepository draftRepository,
    IValidator<RegistrarPickDraftRequestDto> validator) : IRequestHandler<RegistrarPickDraftCommand, DraftResponseDto?>
{
    public async Task<DraftResponseDto?> Handle(RegistrarPickDraftCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var draft = await draftRepository.GetByIdAsync(command.DraftId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        draft.RegistrarPick(command.Request.JogadorId);
        await draftRepository.SaveChangesAsync(cancellationToken);

        var updated = await draftRepository.GetByIdAsync(command.DraftId, cancellationToken) ?? draft;
        return DraftResponseDto.FromEntity(updated);
    }
}
