using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Validators;

public sealed class RegistrarPickDraftValidator : AbstractValidator<RegistrarPickDraftRequestDto>
{
    public RegistrarPickDraftValidator()
    {
        RuleFor(request => request.JogadorId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftInvalidPlayer);
    }
}
