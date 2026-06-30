using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

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

        RuleFor(request => request.Motivo)
            .MaximumLength(500)
            .WithMessage(MessageCodes.DraftMontagemSubstitutionReasonMaxLength);
    }
}
