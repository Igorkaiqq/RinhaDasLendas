using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(request => request.Nome).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.Email).NotEmpty().WithMessage(MessageCodes.FieldRequired).EmailAddress().WithMessage(MessageCodes.InvalidEmailFormat).MaximumLength(256).WithMessage(MessageCodes.MaxLengthExceeded);
        RuleFor(request => request.Senha).NotEmpty().WithMessage(MessageCodes.FieldRequired).MinimumLength(8).WithMessage(MessageCodes.WeakPassword).Matches("[0-9]").WithMessage(MessageCodes.WeakPassword);
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.Senha).WithMessage(MessageCodes.PasswordConfirmationMismatch);
    }
}
