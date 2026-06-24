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

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().WithMessage(MessageCodes.FieldRequired).EmailAddress().WithMessage(MessageCodes.InvalidEmailFormat);
        RuleFor(request => request.Senha).NotEmpty().WithMessage(MessageCodes.FieldRequired);
    }
}

public sealed class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator() => RuleFor(request => request.Email).NotEmpty().WithMessage(MessageCodes.FieldRequired).EmailAddress().WithMessage(MessageCodes.InvalidEmailFormat);
}

public sealed class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().WithMessage(MessageCodes.FieldRequired).EmailAddress().WithMessage(MessageCodes.InvalidEmailFormat);
        RuleFor(request => request.Token).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.NovaSenha).NotEmpty().WithMessage(MessageCodes.FieldRequired).MinimumLength(8).WithMessage(MessageCodes.WeakPassword).Matches("[0-9]").WithMessage(MessageCodes.WeakPassword);
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage(MessageCodes.PasswordConfirmationMismatch);
    }
}

public sealed class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(request => request.SenhaAtual).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.NovaSenha).NotEmpty().WithMessage(MessageCodes.FieldRequired).MinimumLength(8).WithMessage(MessageCodes.WeakPassword).Matches("[0-9]").WithMessage(MessageCodes.WeakPassword);
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage(MessageCodes.PasswordConfirmationMismatch);
    }
}

public sealed class UpdateOwnProfileRequestDtoValidator : AbstractValidator<UpdateOwnProfileRequestDto>
{
    public UpdateOwnProfileRequestDtoValidator() => RuleFor(request => request.Nome).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
}
