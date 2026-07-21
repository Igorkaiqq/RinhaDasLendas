using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class AdicionarPresencaManualDraftMontagemValidator : AbstractValidator<AdicionarPresencaManualDraftMontagemRequestDto>
{
    public AdicionarPresencaManualDraftMontagemValidator()
    {
        RuleFor(request => request.JogadorId)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired);
    }
}
