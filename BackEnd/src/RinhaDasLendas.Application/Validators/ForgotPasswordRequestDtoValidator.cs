using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator() => RuleFor(request => request.Email).NotEmpty().WithMessage(MessageCodes.FieldRequired).EmailAddress().WithMessage(MessageCodes.InvalidEmailFormat);
}
