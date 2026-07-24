using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class AgendamentoPresencaRequestValidator : AbstractValidator<SaveAgendamentoPresencaRequestDto>
{
    public AgendamentoPresencaRequestValidator()
    {
        RuleFor(item => item.Nome)
            .NotEmpty().WithErrorCode(MessageCodes.PresenceScheduleNameRequired).WithMessage(MessageCodes.PresenceScheduleNameRequired);
        RuleFor(item => item.Nome)
            .Must(name => string.IsNullOrWhiteSpace(name) || name.Trim().Length is >= 3 and <= 100)
            .WithErrorCode(MessageCodes.PresenceScheduleNameLengthInvalid)
            .WithMessage(MessageCodes.PresenceScheduleNameLengthInvalid);
        RuleFor(item => item.Observacao)
            .MaximumLength(500)
            .WithErrorCode(MessageCodes.PresenceScheduleObservationTooLong)
            .WithMessage(MessageCodes.PresenceScheduleObservationTooLong);
        RuleFor(item => item.DiasSemana)
            .NotNull()
            .Must(days => days is { Count: > 0 } && days.All(IsValidDay))
            .WithErrorCode(MessageCodes.PresenceScheduleDayRequired)
            .WithMessage(MessageCodes.PresenceScheduleDayRequired);
        RuleFor(item => item.DiasSemana)
            .Must(days => days is null || days.Distinct().Count() == days.Count)
            .WithErrorCode(MessageCodes.PresenceScheduleDayDuplicated)
            .WithMessage(MessageCodes.PresenceScheduleDayDuplicated);
        RuleFor(item => item)
            .Must(item => HasMinutePrecision(item.HorarioPublicacao)
                && HasMinutePrecision(item.HorarioEncerramento)
                && item.HorarioEncerramento > item.HorarioPublicacao)
            .WithErrorCode(MessageCodes.PresenceScheduleTimeRangeInvalid)
            .WithMessage(MessageCodes.PresenceScheduleTimeRangeInvalid);
    }

    private static bool IsValidDay(DiaSemanaIso day) => (int)day is >= 1 and <= 7;

    private static bool HasMinutePrecision(TimeOnly time) => time.Ticks % TimeSpan.TicksPerMinute == 0;
}
