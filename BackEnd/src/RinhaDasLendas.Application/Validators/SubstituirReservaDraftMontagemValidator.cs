using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class SubstituirReservaDraftMontagemValidator : AbstractValidator<SubstituirReservaDraftMontagemRequestDto>
{
    public SubstituirReservaDraftMontagemValidator()
    {
        RuleFor(request => request.TimeId)
            .NotEmpty()
            .WithMessage(MessageCodes.TeamNotFound);

        RuleFor(request => request.JogadorSaiuId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftMontagemPlayerNotInTeam);

        RuleFor(request => request.ReservaEntrouId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftMontagemReserveRequired);

        RuleFor(request => request.NovoCapitaoId)
            .Must(novoCapitaoId => novoCapitaoId != Guid.Empty)
            .When(request => request.NovoCapitaoId.HasValue)
            .WithMessage(MessageCodes.DraftMontagemCaptainsRequired);

        RuleFor(request => request.Motivo)
            .MaximumLength(500)
            .WithMessage(MessageCodes.DraftMontagemSubstitutionReasonMaxLength);
    }
}
