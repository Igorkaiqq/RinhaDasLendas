using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class RegistrarFalhaPublicacaoDiscordDraftMontagemValidator : AbstractValidator<RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto>
{
    public RegistrarFalhaPublicacaoDiscordDraftMontagemValidator()
    {
        RuleFor(request => request.Tipo)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .Must(tipo => DraftMontagemPublicacaoDiscordTipoParser.TryParse(tipo, out _)).WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.ClaimId).NotEmpty().WithMessage(MessageCodes.DiscordPublicationClaimInvalid);
        RuleFor(request => request.DiscordGuildId).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.DiscordChannelId).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.ErroCodigo).MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
    }
}
