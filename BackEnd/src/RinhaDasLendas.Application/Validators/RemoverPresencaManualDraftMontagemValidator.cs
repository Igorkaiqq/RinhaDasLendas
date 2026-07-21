using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class RemoverPresencaManualDraftMontagemValidator : AbstractValidator<RemoverPresencaManualDraftMontagemRequestDto>
{
    public RemoverPresencaManualDraftMontagemValidator()
    {
        RuleFor(request => request.JogadorId)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired);

        RuleFor(request => request.Motivo)
            .NotEmpty()
            .WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(500)
            .WithMessage(MessageCodes.CancellationReasonMaxLength);
    }
}
