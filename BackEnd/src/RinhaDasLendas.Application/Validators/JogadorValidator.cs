using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class JogadorCreateRequestDtoValidator : AbstractValidator<JogadorCreateRequestDto>
{
    public JogadorCreateRequestDtoValidator()
    {
        Include(new JogadorDadosBasicosValidator());

        RuleFor(request => request.Preferencias)
            .SetValidator(new PreferenciasRotasValidator());
    }
}

public sealed class JogadorUpdateRequestDtoValidator : AbstractValidator<JogadorUpdateRequestDto>
{
    public JogadorUpdateRequestDtoValidator()
    {
        Include(new JogadorDadosBasicosValidator());
    }
}

public sealed class UpdatePreferenciasRotasRequestDtoValidator : AbstractValidator<UpdatePreferenciasRotasRequestDto>
{
    public UpdatePreferenciasRotasRequestDtoValidator()
    {
        RuleFor(request => request.Preferencias)
            .SetValidator(new PreferenciasRotasValidator());
    }
}

internal sealed class JogadorDadosBasicosValidator : AbstractValidator<IJogadorDadosBasicos>
{
    public JogadorDadosBasicosValidator()
    {
        RuleFor(request => request.NomeExibicao)
            .NotEmpty()
            .WithMessage("Nome e obrigatorio.")
            .MaximumLength(100)
            .WithMessage("Nome deve ter no maximo 100 caracteres.");

        RuleFor(request => request.Discord)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Informe o Discord do jogador.")
            .MaximumLength(120)
            .WithMessage("Discord deve ter no maximo 120 caracteres.");

        RuleFor(request => request.Elo)
            .Must(EloValido)
            .WithMessage("Selecione um Elo.");

        RuleFor(request => request.Divisao)
            .Must(DivisaoValida)
            .WithMessage("Selecione uma Divisao.")
            .When(request => EloExigeDivisao(request.Elo));

        RuleFor(request => request.Divisao)
            .Must(value => string.IsNullOrWhiteSpace(value))
            .WithMessage("Nao informe Divisao para este Elo.")
            .When(request => EloNaoExigeDivisao(request.Elo));

        RuleFor(request => request.OpGgUrl)
            .Must(url => UrlDeDominioValido(url, "op.gg"))
            .WithMessage("Link de OP.GG deve ser uma URL valida do dominio op.gg.")
            .When(request => !string.IsNullOrWhiteSpace(request.OpGgUrl));

        RuleFor(request => request.DeepLolUrl)
            .Must(url => UrlDeDominioValido(url, "deeplol.gg"))
            .WithMessage("Link de Deeplol deve ser uma URL valida do dominio deeplol.gg.")
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

internal sealed class PreferenciasRotasValidator : AbstractValidator<IReadOnlyCollection<PreferenciaRotaDto>>
{
    public PreferenciasRotasValidator()
    {
        RuleFor(preferencias => preferencias)
            .NotNull()
            .WithMessage("Informe as preferencias de rota.")
            .Must(preferencias => preferencias.Count == 5)
            .WithMessage("Informe exatamente cinco preferencias de rota.")
            .Must(TodasRotasValidas)
            .WithMessage("Informe apenas rotas validas: Top, Jungle, Mid, Adc e Support.")
            .Must(preferencias => preferencias.Select(preferencia => preferencia.Rota).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 5)
            .WithMessage("Cada rota deve aparecer uma unica vez.")
            .Must(preferencias => preferencias.Select(preferencia => preferencia.Prioridade).Distinct().Count() == 5)
            .WithMessage("Cada prioridade deve ser unica.")
            .Must(preferencias => preferencias.All(preferencia => preferencia.Prioridade is >= 1 and <= 5))
            .WithMessage("Prioridades devem estar entre 1 e 5.")
            .Must(preferencias => preferencias.Count(preferencia => preferencia.NaoJogoNemLascando) <= 1)
            .WithMessage("Apenas uma rota pode ser marcada como nao jogo nem lascando.");
    }

    private static bool TodasRotasValidas(IReadOnlyCollection<PreferenciaRotaDto> preferencias)
    {
        return preferencias.All(preferencia => Enum.TryParse<Rota>(preferencia.Rota, ignoreCase: true, out _));
    }
}

public interface IJogadorDadosBasicos
{
    string NomeExibicao { get; }
    string? Discord { get; }
    string? Elo { get; }
    string? Divisao { get; }
    string? OpGgUrl { get; }
    string? DeepLolUrl { get; }
}
