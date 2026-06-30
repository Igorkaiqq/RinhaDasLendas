using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class EncerrarPresencaDraftMontagemValidator : AbstractValidator<EncerrarPresencaDraftMontagemRequestDto>
{
    public EncerrarPresencaDraftMontagemValidator()
    {
        RuleFor(request => request.TamanhoEquipe)
            .InclusiveBetween(DraftMontagem.MinimoTamanhoEquipe, DraftMontagem.MaximoTamanhoEquipe)
            .WithMessage(MessageCodes.TeamSizeRange);
    }
}
