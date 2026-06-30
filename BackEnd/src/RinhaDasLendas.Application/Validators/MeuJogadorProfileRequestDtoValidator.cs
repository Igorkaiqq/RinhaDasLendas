using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class MeuJogadorProfileRequestDtoValidator : AbstractValidator<MeuJogadorProfileRequestDto>
{
    public MeuJogadorProfileRequestDtoValidator()
    {
        Include(new JogadorDadosBasicosValidator());

        RuleFor(request => request.RiotId)
            .NotEmpty()
            .WithMessage(MessageCodes.LeagueNicknameRequired)
            .MaximumLength(120)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.OpGgUrl)
            .NotEmpty()
            .WithMessage(MessageCodes.InvalidOpGgLink);

        RuleFor(request => request.DeepLolUrl)
            .NotEmpty()
            .WithMessage(MessageCodes.InvalidDeeplolLink);

        RuleFor(request => request.Preferencias)
            .SetValidator(new PreferenciasRotasValidator());
    }
}
