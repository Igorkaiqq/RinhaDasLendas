using FluentValidation;
using RinhaDasLendas.Application.Commands.Series;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateSeriesRequestDtoValidator : AbstractValidator<CreateSeriesRequestDto>
{
    public CreateSeriesRequestDtoValidator()
    {
        RuleFor(request => request.SeasonId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.VersaoRegrasId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Tipo)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Tipo.Value!.Value)
            .IsInEnum().WithMessage(MessageCodes.ValidationError)
            .When(request => request.Tipo.HasValue && request.Tipo.Value.HasValue);
        RuleFor(request => request.AgendadaPara).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.LadoOrigemIds)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .Must(ids => ids is { Count: 2 }
                && ids.All(id => id != Guid.Empty)
                && ids.Distinct().Count() == 2)
            .WithMessage(MessageCodes.DailySeriesSidesInvalid);

        When(request => request.Tipo.Value == SerieTipo.DiariaTemporaria, () =>
        {
            RuleFor(request => request.CompeticaoId)
                .Must(PresentGuid).WithMessage(MessageCodes.DailyCircuitCompetitionInvalid);
            RuleFor(request => request.RodadaId)
                .Must(PresentGuid).WithMessage(MessageCodes.RoundSeasonMismatch);
            RuleFor(request => request.DraftMontagemId)
                .Must(PresentGuid).WithMessage(MessageCodes.DailySeriesDraftInvalid);
            RuleFor(request => request.DataLocal)
                .Must(value => value.HasValue && value.Value.HasValue)
                .WithMessage(MessageCodes.DailySeriesDateInvalid);
        });
    }

    private static bool PresentGuid(Optional<Guid?> value) =>
        value.HasValue && value.Value.HasValue && value.Value.Value != Guid.Empty;
}

public sealed class ReasonRequestDtoValidator : AbstractValidator<ReasonRequestDto>
{
    public ReasonRequestDtoValidator()
    {
        RuleFor(request => request.Justificativa)
            .NotEmpty().WithMessage(MessageCodes.CorrectionJustificationRequired)
            .MaximumLength(500).WithMessage(MessageCodes.MaxLengthExceeded);
    }
}

public sealed class CreateSeriesCommandValidator : AbstractValidator<CreateSeriesCommand>
{
    public CreateSeriesCommandValidator(IValidator<CreateSeriesRequestDto> requestValidator) =>
        RuleFor(command => command.Request)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .SetValidator(requestValidator);
}

public sealed class StartSeriesCommandValidator : AbstractValidator<StartSeriesCommand>
{
    public StartSeriesCommandValidator()
    {
        RuleFor(command => command.SeriesId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class CancelSeriesCommandValidator : AbstractValidator<CancelSeriesCommand>
{
    public CancelSeriesCommandValidator(IValidator<ReasonRequestDto> requestValidator)
    {
        RuleFor(command => command.SeriesId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .SetValidator(requestValidator);
    }
}

public sealed class AnnulSeriesCommandValidator : AbstractValidator<AnnulSeriesCommand>
{
    public AnnulSeriesCommandValidator(IValidator<ReasonRequestDto> requestValidator)
    {
        RuleFor(command => command.SeriesId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .SetValidator(requestValidator);
    }
}
