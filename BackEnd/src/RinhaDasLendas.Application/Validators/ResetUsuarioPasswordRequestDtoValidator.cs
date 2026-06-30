using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class ResetUsuarioPasswordRequestDtoValidator : AbstractValidator<ResetUsuarioPasswordRequestDto>
{
    public ResetUsuarioPasswordRequestDtoValidator()
    {
        RuleFor(request => request.NovaSenha).NotEmpty().WithMessage(MessageCodes.FieldRequired).MinimumLength(8).WithMessage(MessageCodes.WeakPassword).Matches("[0-9]").WithMessage(MessageCodes.WeakPassword);
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage(MessageCodes.PasswordConfirmationMismatch);
    }
}
