using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class AdquirirClaimPublicacaoDiscordDraftMontagemValidator : AbstractValidator<AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto>
{
    public AdquirirClaimPublicacaoDiscordDraftMontagemValidator()
    {
        RuleFor(request => request.Tipo)
            .NotEmpty()
            .WithMessage(MessageCodes.FieldRequired)
            .Must(tipo => DraftMontagemPublicacaoDiscordTipoParser.TryParse(tipo, out _))
            .WithMessage(MessageCodes.FieldRequired);
    }
}
