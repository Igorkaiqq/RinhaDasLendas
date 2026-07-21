using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class RepublicarPublicacaoDiscordDraftMontagemValidator : AbstractValidator<RepublicarPublicacaoDiscordDraftMontagemRequestDto>
{
    public RepublicarPublicacaoDiscordDraftMontagemValidator()
    {
        RuleFor(request => request.Tipo)
            .IsInEnum()
            .WithMessage(MessageCodes.FieldRequired);

        RuleFor(request => request.Motivo)
            .MaximumLength(500)
            .WithMessage(MessageCodes.CancellationReasonMaxLength);
    }
}
