using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class DiscordConfigurationValidator : AbstractValidator<DiscordConfigurationDto>
{
    public DiscordConfigurationValidator()
    {
        RuleFor(request => request.GuildId).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.PresenceChannelId).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.NewsChannelId).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.AdminChannelId).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.DraftChannelId).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.MatchResultChannelId).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
    }
}
