using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class DefinirCapitaesDraftMontagemValidator : AbstractValidator<DefinirCapitaesDraftMontagemRequestDto>
{
    public DefinirCapitaesDraftMontagemValidator()
    {
        RuleFor(request => request.CapitaesIds)
            .NotNull().WithMessage(MessageCodes.DraftMontagemCaptainsRequired)
            .Must(ids => ids.Count > 0).WithMessage(MessageCodes.DraftMontagemCaptainsRequired)
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage(MessageCodes.DraftMontagemCaptainsDistinct);
    }
}
