using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class RegistrarFalhaPublicacaoDiscordDraftMontagemValidator : AbstractValidator<RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto>
{
    public RegistrarFalhaPublicacaoDiscordDraftMontagemValidator()
    {
        RuleFor(request => request.Tipo)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .Must(IsValidPublicationType).WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.ClaimId).NotEmpty().WithMessage(MessageCodes.DiscordPublicationClaimInvalid);
        RuleFor(request => request.DiscordGuildId).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.DiscordChannelId).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.ErroCodigo).MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
    }

    private static bool IsValidPublicationType(string tipo)
    {
        return Enum.TryParse<DraftMontagemPublicacaoDiscordTipo>(tipo, true, out var parsed) && Enum.IsDefined(parsed);
    }
}
