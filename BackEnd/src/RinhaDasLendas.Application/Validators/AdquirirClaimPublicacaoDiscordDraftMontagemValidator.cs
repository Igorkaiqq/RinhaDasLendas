using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class AdquirirClaimPublicacaoDiscordDraftMontagemValidator : AbstractValidator<AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto>
{
    public AdquirirClaimPublicacaoDiscordDraftMontagemValidator()
    {
        RuleFor(request => request.Tipo)
            .NotEmpty()
            .WithMessage(MessageCodes.FieldRequired)
            .Must(IsValidPublicationType)
            .WithMessage(MessageCodes.FieldRequired);
    }

    private static bool IsValidPublicationType(string tipo)
    {
        return Enum.TryParse<DraftMontagemPublicacaoDiscordTipo>(tipo, true, out var parsed) && Enum.IsDefined(parsed);
    }
}
