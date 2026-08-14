using FluentValidation;
using RinhaDasLendas.Application.Commands.Partidas;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class MatchSidePicksRequestDtoValidator : AbstractValidator<MatchSidePicksRequestDto>
{
    public MatchSidePicksRequestDtoValidator()
    {
        RuleFor(request => request.LadoSerieId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.ChampionIds)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .Must(ids => ids is { Count: 5 }
                && ids.All(id => id > 0)
                && ids.Distinct().Count() == 5)
            .WithMessage(MessageCodes.ChampionRequired);
    }
}

public sealed class RegisterPicksRequestDtoValidator : AbstractValidator<RegisterPicksRequestDto>
{
    public RegisterPicksRequestDtoValidator()
    {
        RuleForEach(request => request.Lados).SetValidator(new MatchSidePicksRequestDtoValidator());
        RuleFor(request => request.Lados)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .Must(lados => lados is { Count: 2 }
                && lados.All(lado => lado is not null))
            .WithMessage(MessageCodes.DailySeriesSidesInvalid)
            .Must(lados => lados!.Select(lado => lado.LadoSerieId).Distinct().Count() == 2)
            .WithMessage(MessageCodes.DailySeriesSidesInvalid)
            .Must(lados => lados!.SelectMany(lado => lado.ChampionIds ?? []).Distinct().Count() == 10)
            .WithMessage(MessageCodes.ChampionRequired);
    }
}

public sealed class ConfirmResultRequestDtoValidator : AbstractValidator<ConfirmResultRequestDto>
{
    public ConfirmResultRequestDtoValidator()
    {
        RuleFor(request => request.LadoVencedorId).NotEmpty().WithMessage(MessageCodes.MatchResultRequired);
        RuleFor(request => request.MotivoTermino)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.MatchResultRequired);
        RuleFor(request => request.MotivoTermino.Value!.Value)
            .IsInEnum().WithMessage(MessageCodes.MatchResultRequired)
            .When(request => request.MotivoTermino.HasValue && request.MotivoTermino.Value.HasValue);
    }
}

public sealed class RemakeRequestDtoValidator : AbstractValidator<RemakeRequestDto>
{
    public RemakeRequestDtoValidator()
    {
        RuleFor(request => request.DecisaoPicks)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.RemakeDecisionRequired);
        RuleFor(request => request.DecisaoPicks.Value!.Value)
            .IsInEnum().WithMessage(MessageCodes.RemakeDecisionRequired)
            .When(request => request.DecisaoPicks.HasValue && request.DecisaoPicks.Value.HasValue);
        RuleFor(request => request.Justificativa)
            .NotEmpty().WithMessage(MessageCodes.CorrectionJustificationRequired)
            .MaximumLength(500).WithMessage(MessageCodes.MaxLengthExceeded);
    }
}

public sealed class AnnulMatchRequestDtoValidator : AbstractValidator<AnnulMatchRequestDto>
{
    public AnnulMatchRequestDtoValidator()
    {
        RuleFor(request => request.Justificativa)
            .NotEmpty().WithMessage(MessageCodes.CorrectionJustificationRequired)
            .MaximumLength(500).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.AnularSerieSeInconclusiva)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
    }
}

public sealed class CorrectMatchRequestDtoValidator : AbstractValidator<CorrectMatchRequestDto>
{
    public CorrectMatchRequestDtoValidator()
    {
        RuleFor(request => request.Justificativa)
            .NotEmpty().WithMessage(MessageCodes.CorrectionJustificationRequired)
            .MaximumLength(500).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request)
            .Must(HasPicksOrCompleteResult)
            .WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request)
            .Must(HasCompleteResultPairWhenSupplied)
            .WithMessage(MessageCodes.MatchResultRequired);
        RuleFor(request => request.Lados.Value)
            .Must(lados => lados is null
                || new RegisterPicksRequestDtoValidator()
                    .Validate(new RegisterPicksRequestDto(lados)).IsValid)
            .WithMessage(MessageCodes.ChampionRequired)
            .When(request => request.Lados.HasValue);
        RuleFor(request => request.LadoVencedorId.Value)
            .NotNull().WithMessage(MessageCodes.MatchResultRequired)
            .NotEqual(Guid.Empty).WithMessage(MessageCodes.MatchResultRequired)
            .When(request => request.LadoVencedorId.HasValue);
        RuleFor(request => request.MotivoTermino.Value)
            .NotNull().WithMessage(MessageCodes.MatchResultRequired)
            .Must(value => value.HasValue && Enum.IsDefined(value.Value))
            .WithMessage(MessageCodes.MatchResultRequired)
            .When(request => request.MotivoTermino.HasValue);
        RuleFor(request => request.AnularSerieSeInconclusiva.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .When(request => request.AnularSerieSeInconclusiva.HasValue);
    }

    private static bool HasPicksOrCompleteResult(CorrectMatchRequestDto request)
    {
        var hasPicks = request.Lados.HasValue && request.Lados.Value is not null;
        var hasWinner = request.LadoVencedorId.HasValue;
        var hasReason = request.MotivoTermino.HasValue;
        return hasPicks || hasWinner && hasReason;
    }

    private static bool HasCompleteResultPairWhenSupplied(CorrectMatchRequestDto request) =>
        request.LadoVencedorId.HasValue == request.MotivoTermino.HasValue;
}

public sealed class AddNextMatchCommandValidator : AbstractValidator<AddNextMatchCommand>
{
    public AddNextMatchCommandValidator()
    {
        RuleFor(command => command.SeriesId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class RegisterMatchPicksCommandValidator : AbstractValidator<RegisterMatchPicksCommand>
{
    public RegisterMatchPicksCommandValidator(IValidator<RegisterPicksRequestDto> requestValidator) =>
        Configure(this, requestValidator);

    private static void Configure(
        AbstractValidator<RegisterMatchPicksCommand> validator,
        IValidator<RegisterPicksRequestDto> requestValidator)
    {
        validator.RuleFor(command => command.MatchId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        validator.RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        validator.RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class ConfirmMatchResultCommandValidator : AbstractValidator<ConfirmMatchResultCommand>
{
    public ConfirmMatchResultCommandValidator(IValidator<ConfirmResultRequestDto> requestValidator)
    {
        RuleFor(command => command.MatchId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class MarkMatchRemakeCommandValidator : AbstractValidator<MarkMatchRemakeCommand>
{
    public MarkMatchRemakeCommandValidator(IValidator<RemakeRequestDto> requestValidator)
    {
        RuleFor(command => command.MatchId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class AnnulMatchCommandValidator : AbstractValidator<AnnulMatchCommand>
{
    public AnnulMatchCommandValidator(IValidator<AnnulMatchRequestDto> requestValidator)
    {
        RuleFor(command => command.MatchId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class CorrectMatchCommandValidator : AbstractValidator<CorrectMatchCommand>
{
    public CorrectMatchCommandValidator(IValidator<CorrectMatchRequestDto> requestValidator)
    {
        RuleFor(command => command.MatchId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}
