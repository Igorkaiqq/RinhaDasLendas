using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class UpdateUsuarioRequestDtoValidator : AbstractValidator<UpdateUsuarioRequestDto>
{
    public UpdateUsuarioRequestDtoValidator() => RuleFor(request => request.Nome).NotEmpty().WithMessage(MessageCodes.FieldRequired).MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);
}

public sealed class UpdateUsuarioRolesRequestDtoValidator : AbstractValidator<UpdateUsuarioRolesRequestDto>
{
    public UpdateUsuarioRolesRequestDtoValidator()
    {
        RuleFor(request => request.Roles).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleForEach(request => request.Roles).NotEmpty().WithMessage(MessageCodes.FieldRequired);
    }
}

public sealed class ResetUsuarioPasswordRequestDtoValidator : AbstractValidator<ResetUsuarioPasswordRequestDto>
{
    public ResetUsuarioPasswordRequestDtoValidator()
    {
        RuleFor(request => request.NovaSenha).NotEmpty().WithMessage(MessageCodes.FieldRequired).MinimumLength(8).WithMessage(MessageCodes.WeakPassword).Matches("[0-9]").WithMessage(MessageCodes.WeakPassword);
        RuleFor(request => request.ConfirmacaoSenha).Equal(request => request.NovaSenha).WithMessage(MessageCodes.PasswordConfirmationMismatch);
    }
}
