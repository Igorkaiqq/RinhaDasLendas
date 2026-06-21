using FluentValidation;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Validators;

public sealed class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(request => request.Nome).NotEmpty().MaximumLength(120);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Senha).NotEmpty().MinimumLength(8).Matches("[0-9]").WithMessage("Senha deve conter ao menos um numero.");
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.Senha).WithMessage("Confirmacao de senha nao confere.");
    }
}

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Senha).NotEmpty();
    }
}

public sealed class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator() => RuleFor(request => request.Email).NotEmpty().EmailAddress();
}

public sealed class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Token).NotEmpty();
        RuleFor(request => request.NovaSenha).NotEmpty().MinimumLength(8).Matches("[0-9]").WithMessage("Senha deve conter ao menos um numero.");
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage("Confirmacao de senha nao confere.");
    }
}

public sealed class ChangePasswordRequestDtoValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestDtoValidator()
    {
        RuleFor(request => request.SenhaAtual).NotEmpty();
        RuleFor(request => request.NovaSenha).NotEmpty().MinimumLength(8).Matches("[0-9]").WithMessage("Senha deve conter ao menos um numero.");
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage("Confirmacao de senha nao confere.");
    }
}

public sealed class UpdateOwnProfileRequestDtoValidator : AbstractValidator<UpdateOwnProfileRequestDto>
{
    public UpdateOwnProfileRequestDtoValidator() => RuleFor(request => request.Nome).NotEmpty().MaximumLength(120);
}
