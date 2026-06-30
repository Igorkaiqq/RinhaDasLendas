using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class RegistrarPickDraftMontagemValidator : AbstractValidator<RegistrarPickDraftMontagemRequestDto>
{
    public RegistrarPickDraftMontagemValidator()
    {
        RuleFor(request => request.JogadorId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftInvalidPlayer);
    }
}
