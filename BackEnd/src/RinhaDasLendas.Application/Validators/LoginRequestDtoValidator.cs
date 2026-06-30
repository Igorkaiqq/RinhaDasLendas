using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().WithMessage(MessageCodes.FieldRequired).EmailAddress().WithMessage(MessageCodes.InvalidEmailFormat);
        RuleFor(request => request.Senha).NotEmpty().WithMessage(MessageCodes.FieldRequired);
    }
}
