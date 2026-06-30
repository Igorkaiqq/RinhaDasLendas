using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

internal sealed class PreferenciasRotasValidator : AbstractValidator<IReadOnlyCollection<PreferenciaRotaDto>>
{
    public PreferenciasRotasValidator()
    {
        RuleFor(preferencias => preferencias)
            .NotNull()
            .WithMessage(MessageCodes.RoutesRequired)
            .Must(preferencias => preferencias.Count == 5)
            .WithMessage(MessageCodes.RoutesRequired)
            .Must(TodasRotasValidas)
            .WithMessage(MessageCodes.InvalidRoute)
            .Must(preferencias => preferencias.Select(preferencia => preferencia.Rota).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 5)
            .WithMessage(MessageCodes.DuplicateRoute)
            .Must(preferencias => preferencias.Select(preferencia => preferencia.Prioridade).Distinct().Count() == 5)
            .WithMessage(MessageCodes.RoutePrioritiesMustBeUnique)
            .Must(preferencias => preferencias.All(preferencia => preferencia.Prioridade is >= 1 and <= 5))
            .WithMessage(MessageCodes.RoutePrioritiesRange)
            .Must(preferencias => preferencias.Count(preferencia => preferencia.NaoJogoNemLascando) <= 1)
            .WithMessage(MessageCodes.OnlyOneNeverPlayRole);
    }

    private static bool TodasRotasValidas(IReadOnlyCollection<PreferenciaRotaDto> preferencias)
    {
        return preferencias.All(preferencia => Enum.TryParse<Rota>(preferencia.Rota, ignoreCase: true, out _));
    }
}
