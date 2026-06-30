using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

internal sealed class JogadorDadosBasicosValidator : AbstractValidator<IJogadorDadosBasicos>
{
    public JogadorDadosBasicosValidator()
    {
        RuleFor(request => request.NomeExibicao)
            .NotEmpty()
            .WithMessage(MessageCodes.PlayerNameRequired)
            .MaximumLength(100)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.Discord)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(120)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.Elo)
            .Must(EloValido)
            .WithMessage(MessageCodes.FieldRequired);

        RuleFor(request => request.Divisao)
            .Must(DivisaoValida)
            .WithMessage(MessageCodes.FieldRequired)
            .When(request => EloExigeDivisao(request.Elo));

        RuleFor(request => request.Divisao)
            .Must(value => string.IsNullOrWhiteSpace(value))
            .WithMessage(MessageCodes.DivisionNotAllowedForElo)
            .When(request => EloNaoExigeDivisao(request.Elo));

        RuleFor(request => request.OpGgUrl)
            .Must(url => UrlDeDominioValido(url, "op.gg"))
            .WithMessage(MessageCodes.InvalidOpGgLink)
            .When(request => !string.IsNullOrWhiteSpace(request.OpGgUrl));

        RuleFor(request => request.DeepLolUrl)
            .Must(url => UrlDeDominioValido(url, "deeplol.gg"))
            .WithMessage(MessageCodes.InvalidDeeplolLink)
            .When(request => !string.IsNullOrWhiteSpace(request.DeepLolUrl));
    }

    private static bool EloValido(string? value)
    {
        return EloExtensions.TryParseDisplayName(value, out _);
    }

    private static bool DivisaoValida(string? value)
    {
        return DivisaoExtensions.TryParseDisplayName(value, out _);
    }

    private static bool EloExigeDivisao(string? value)
    {
        return EloExtensions.TryParseDisplayName(value, out var elo) && elo.ExigeDivisao();
    }

    private static bool EloNaoExigeDivisao(string? value)
    {
        return EloExtensions.TryParseDisplayName(value, out var elo) && !elo.ExigeDivisao();
    }

    private static bool UrlDeDominioValido(string? value, string dominio)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && uri.Host.EndsWith(dominio, StringComparison.OrdinalIgnoreCase);
    }
}
