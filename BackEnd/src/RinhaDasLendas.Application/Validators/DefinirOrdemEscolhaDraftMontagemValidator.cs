using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class DefinirOrdemEscolhaDraftMontagemValidator : AbstractValidator<DefinirOrdemEscolhaDraftMontagemRequestDto>
{
    public DefinirOrdemEscolhaDraftMontagemValidator()
    {
        RuleFor(request => request.Modo)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .Must(value => Enum.TryParse<DraftMontagemOrdemEscolhaModo>(value, true, out _)).WithMessage(MessageCodes.DraftMontagemPickOrderInvalid);
    }
}
