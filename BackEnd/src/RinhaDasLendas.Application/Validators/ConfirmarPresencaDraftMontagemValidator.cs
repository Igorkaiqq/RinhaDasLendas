using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class ConfirmarPresencaDraftMontagemValidator : AbstractValidator<ConfirmarPresencaDraftMontagemRequestDto>
{
    public ConfirmarPresencaDraftMontagemValidator()
    {
        RuleFor(request => request.Origem)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .Must(value => Enum.TryParse<DraftMontagemPresencaOrigem>(value, true, out _)).WithMessage(MessageCodes.FieldRequired);
    }
}
