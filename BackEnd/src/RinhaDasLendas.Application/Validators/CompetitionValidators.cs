using FluentValidation;
using RinhaDasLendas.Application.Commands.Competicoes;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Competicoes;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateCompetitionRequestDtoValidator : AbstractValidator<CreateCompetitionRequestDto>
{
    public CreateCompetitionRequestDtoValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.Codigo)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.CircuitoDiario)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
    }
}

public sealed class UpdateCompetitionRequestDtoValidator : AbstractValidator<UpdateCompetitionRequestDto>
{
    public UpdateCompetitionRequestDtoValidator()
    {
        RuleFor(request => request)
            .Must(request => request.Nome.HasValue
                || request.Codigo.HasValue
                || request.CircuitoDiario.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Nome.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded)
            .When(request => request.Nome.HasValue);
        RuleFor(request => request.Codigo.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded)
            .When(request => request.Codigo.HasValue);
        RuleFor(request => request.CircuitoDiario.Value)
            .NotNull().WithMessage(MessageCodes.FieldRequired)
            .When(request => request.CircuitoDiario.HasValue);
    }
}

public sealed class CreateRoundRequestDtoValidator : AbstractValidator<CreateRoundRequestDto>
{
    public CreateRoundRequestDtoValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .MaximumLength(80).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.Ordem)
            .GreaterThan(0).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class ReorderRoundsRequestDtoValidator : AbstractValidator<ReorderRoundsRequestDto>
{
    public ReorderRoundsRequestDtoValidator()
    {
        RuleFor(request => request.RodadaIds)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .Must(ids => ids is not null && ids.All(id => id != Guid.Empty))
            .WithMessage(MessageCodes.ValidationError)
            .Must(ids => ids is not null && ids.Distinct().Count() == ids.Count)
            .WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class PublishRulesRequestDtoValidator : AbstractValidator<PublishRulesRequestDto>
{
    public PublishRulesRequestDtoValidator()
    {
        RuleFor(request => request.Formato)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Formato.Value!.Value)
            .IsInEnum().WithMessage(MessageCodes.SeriesMustBeBestOfThreeOrFive)
            .When(request => request.Formato.HasValue && request.Formato.Value.HasValue);
        RuleFor(request => request.ModoDraft)
            .Must(value => value.HasValue && value.Value.HasValue)
            .WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.ModoDraft.Value!.Value)
            .IsInEnum().WithMessage(MessageCodes.ValidationError)
            .When(request => request.ModoDraft.HasValue && request.ModoDraft.Value.HasValue);
    }
}

public sealed class CreateCompetitionCommandValidator : AbstractValidator<CreateCompetitionCommand>
{
    public CreateCompetitionCommandValidator(IValidator<CreateCompetitionRequestDto> requestValidator)
    {
        RuleFor(command => command.SeasonId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class UpdateCompetitionCommandValidator : AbstractValidator<UpdateCompetitionCommand>
{
    public UpdateCompetitionCommandValidator(IValidator<UpdateCompetitionRequestDto> requestValidator)
    {
        RuleFor(command => command.CompetitionId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class CreateRoundCommandValidator : AbstractValidator<CreateRoundCommand>
{
    public CreateRoundCommandValidator(IValidator<CreateRoundRequestDto> requestValidator)
    {
        RuleFor(command => command.CompetitionId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class ReorderRoundsCommandValidator : AbstractValidator<ReorderRoundsCommand>
{
    public ReorderRoundsCommandValidator(IValidator<ReorderRoundsRequestDto> requestValidator)
    {
        RuleFor(command => command.CompetitionId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class PublishSeasonRulesCommandValidator : AbstractValidator<PublishSeasonRulesCommand>
{
    public PublishSeasonRulesCommandValidator(IValidator<PublishRulesRequestDto> requestValidator)
    {
        RuleFor(command => command.SeasonId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class PublishCompetitionRulesCommandValidator : AbstractValidator<PublishCompetitionRulesCommand>
{
    public PublishCompetitionRulesCommandValidator(IValidator<PublishRulesRequestDto> requestValidator)
    {
        RuleFor(command => command.CompetitionId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(command => command.ExpectedVersion).GreaterThanOrEqualTo(0).WithMessage(MessageCodes.ValidationError);
        RuleFor(command => command.Request).NotNull().WithMessage(MessageCodes.FieldRequired).SetValidator(requestValidator);
    }
}

public sealed class GetCompeticoesQueryValidator : AbstractValidator<GetCompeticoesQuery>
{
    public GetCompeticoesQueryValidator()
    {
        RuleFor(query => query.Selecao)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(MessageCodes.ValidationError)
            .Must(selecao => Enum.IsDefined(selecao.Tipo)).WithMessage(MessageCodes.ValidationError);
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage(MessageCodes.ValidationError);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class GetSeasonCompeticoesQueryValidator : AbstractValidator<GetSeasonCompeticoesQuery>
{
    public GetSeasonCompeticoesQueryValidator()
    {
        RuleFor(query => query.SeasonId)
            .NotEmpty().WithMessage(MessageCodes.ValidationError);
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage(MessageCodes.ValidationError);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100).WithMessage(MessageCodes.ValidationError);
    }
}

public sealed class GetCompeticaoByIdQueryValidator : AbstractValidator<GetCompeticaoByIdQuery>
{
    public GetCompeticaoByIdQueryValidator()
    {
        RuleFor(query => query.CompetitionId)
            .NotEmpty().WithMessage(MessageCodes.ValidationError);
    }
}
