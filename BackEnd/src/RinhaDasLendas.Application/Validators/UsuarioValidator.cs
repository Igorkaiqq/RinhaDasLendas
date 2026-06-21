using FluentValidation;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Validators;

public sealed class UpdateUsuarioRequestDtoValidator : AbstractValidator<UpdateUsuarioRequestDto>
{
    public UpdateUsuarioRequestDtoValidator() => RuleFor(request => request.Nome).NotEmpty().MaximumLength(120);
}

public sealed class UpdateUsuarioRolesRequestDtoValidator : AbstractValidator<UpdateUsuarioRolesRequestDto>
{
    public UpdateUsuarioRolesRequestDtoValidator()
    {
        RuleFor(request => request.Roles).NotEmpty();
        RuleForEach(request => request.Roles).NotEmpty();
    }
}

public sealed class ResetUsuarioPasswordRequestDtoValidator : AbstractValidator<ResetUsuarioPasswordRequestDto>
{
    public ResetUsuarioPasswordRequestDtoValidator()
    {
        RuleFor(request => request.NovaSenha).NotEmpty().MinimumLength(8).Matches("[0-9]").WithMessage("Senha deve conter ao menos um numero.");
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage("Confirmacao de senha nao confere.");
    }
}
