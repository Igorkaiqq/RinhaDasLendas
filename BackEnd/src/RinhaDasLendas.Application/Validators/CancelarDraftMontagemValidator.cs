using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class CancelarDraftMontagemValidator : AbstractValidator<CancelarDraftMontagemRequestDto>
{
    public CancelarDraftMontagemValidator()
    {
        RuleFor(request => request.Motivo)
            .NotEmpty()
            .WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(500)
            .WithMessage(MessageCodes.CancellationReasonMaxLength);
    }
}
