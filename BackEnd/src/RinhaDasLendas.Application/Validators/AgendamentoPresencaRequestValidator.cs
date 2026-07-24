using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Application.Validators;

public sealed class AgendamentoPresencaRequestValidator : AbstractValidator<SaveAgendamentoPresencaRequestDto>
{
    public AgendamentoPresencaRequestValidator()
    {
        RuleFor(item => item.Nome)
            .Must(name => AgendamentoPresencaRules.NormalizeName(name).Length > 0)
            .WithErrorCode(MessageCodes.PresenceScheduleNameRequired)
            .WithMessage(MessageCodes.PresenceScheduleNameRequired);
        RuleFor(item => item.Nome)
            .Must(name =>
            {
                var normalized = AgendamentoPresencaRules.NormalizeName(name);
                return normalized.Length == 0 || AgendamentoPresencaRules.HasValidNameLength(normalized);
            })
            .WithErrorCode(MessageCodes.PresenceScheduleNameLengthInvalid)
            .WithMessage(MessageCodes.PresenceScheduleNameLengthInvalid);
        RuleFor(item => item.Observacao)
            .Must(observation => AgendamentoPresencaRules.HasValidObservationLength(
                AgendamentoPresencaRules.NormalizeObservation(observation)))
            .WithErrorCode(MessageCodes.PresenceScheduleObservationTooLong)
            .WithMessage(MessageCodes.PresenceScheduleObservationTooLong);
        RuleFor(item => item.DiasSemana)
            .Must(AgendamentoPresencaRules.HasValidDays)
            .WithErrorCode(MessageCodes.PresenceScheduleDayRequired)
            .WithMessage(MessageCodes.PresenceScheduleDayRequired);
        RuleFor(item => item.DiasSemana)
            .Must(AgendamentoPresencaRules.HasUniqueDays)
            .WithErrorCode(MessageCodes.PresenceScheduleDayDuplicated)
            .WithMessage(MessageCodes.PresenceScheduleDayDuplicated);
        RuleFor(item => item)
            .Must(item => AgendamentoPresencaRules.HasValidTimeRange(
                item.HorarioPublicacao,
                item.HorarioEncerramento))
            .WithErrorCode(MessageCodes.PresenceScheduleTimeRangeInvalid)
            .WithMessage(MessageCodes.PresenceScheduleTimeRangeInvalid);
    }
}
