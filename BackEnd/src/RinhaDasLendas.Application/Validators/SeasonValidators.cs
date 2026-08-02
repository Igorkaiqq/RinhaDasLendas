using FluentValidation;
using RinhaDasLendas.Application.Commands.Seasons;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Seasons;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateSeasonRequestDtoValidator : AbstractValidator<CreateSeasonRequestDto>
{
    public CreateSeasonRequestDtoValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty().WithMessage(MessageCodes.SeasonNameRequired)
            .MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.Ano)
            .InclusiveBetween(2009, 9999).WithMessage(MessageCodes.ValidationError);
        RuleFor(request => request.OrdemNoAno)
            .GreaterThan(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(request => request)
            .Must(request => request.DataFimExclusiva > request.DataInicio)
            .WithMessage(MessageCodes.SeasonPeriodInvalid);
    }
}

public sealed class UpdateSeasonRequestDtoValidator : AbstractValidator<UpdateSeasonRequestDto>
{
    public UpdateSeasonRequestDtoValidator()
    {
        RuleFor(request => request)
            .Must(request => request.Nome.HasValue
                || request.Ano.HasValue
                || request.OrdemNoAno.HasValue
                || request.DataInicio.HasValue
                || request.DataFimExclusiva.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Nome.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .NotEmpty().WithMessage(MessageCodes.SeasonNameRequired)
            .MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded)
            .When(request => request.Nome.HasValue);
        RuleFor(request => request.Ano.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .InclusiveBetween(2009, 9999).WithMessage(MessageCodes.ValidationError)
            .When(request => request.Ano.HasValue);
        RuleFor(request => request.OrdemNoAno.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .GreaterThan(0).WithMessage(MessageCodes.ValidationError)
            .When(request => request.OrdemNoAno.HasValue);
        RuleFor(request => request.DataInicio.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .When(request => request.DataInicio.HasValue);
        RuleFor(request => request.DataFimExclusiva.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .When(request => request.DataFimExclusiva.HasValue);
        RuleFor(request => request)
            .Must(request => request.DataFimExclusiva.Value > request.DataInicio.Value)
            .WithMessage(MessageCodes.SeasonPeriodInvalid)
            .When(request => request.DataInicio.HasValue && request.DataFimExclusiva.HasValue);
    }
}

public sealed class CreateSeasonCommandValidator : AbstractValidator<CreateSeasonCommand>
{
    public CreateSeasonCommandValidator(IValidator<CreateSeasonRequestDto> requestValidator)
    {
        RuleFor(command => command.Request)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .SetValidator(requestValidator);
    }
}

public sealed class UpdateSeasonCommandValidator : AbstractValidator<UpdateSeasonCommand>
{
    public UpdateSeasonCommandValidator(IValidator<UpdateSeasonRequestDto> requestValidator)
    {
        RuleFor(command => command.SeasonId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class AtivarSeasonCommandValidator : AbstractValidator<AtivarSeasonCommand>
{
    public AtivarSeasonCommandValidator()
    {
        RuleFor(command => command.SeasonId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class EncerrarSeasonCommandValidator : AbstractValidator<EncerrarSeasonCommand>
{
    public EncerrarSeasonCommandValidator()
    {
        RuleFor(command => command.SeasonId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class GetSeasonsQueryValidator : AbstractValidator<GetSeasonsQuery>
{
    public GetSeasonsQueryValidator()
    {
        RuleFor(query => query.Estado)
            .Must(estado => !estado.HasValue || Enum.IsDefined(estado.Value))
            .WithMessage(MessageCodes.ValidationError);
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage(MessageCodes.ValidationError);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class GetSeasonByIdQueryValidator : AbstractValidator<GetSeasonByIdQuery>
{
    public GetSeasonByIdQueryValidator()
    {
        RuleFor(query => query.SeasonId)
            .NotEmpty().WithMessage(MessageCodes.ValidationError);
    }
}
