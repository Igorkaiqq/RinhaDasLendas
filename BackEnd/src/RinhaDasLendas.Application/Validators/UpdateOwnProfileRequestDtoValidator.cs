using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class UpdateOwnProfileRequestDtoValidator : AbstractValidator<UpdateOwnProfileRequestDto>
{
    public UpdateOwnProfileRequestDtoValidator() => RuleFor(request => request.Nome).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
}
