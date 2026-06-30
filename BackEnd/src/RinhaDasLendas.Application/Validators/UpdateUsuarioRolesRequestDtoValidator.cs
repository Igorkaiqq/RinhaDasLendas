using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class UpdateUsuarioRolesRequestDtoValidator : AbstractValidator<UpdateUsuarioRolesRequestDto>
{
    public UpdateUsuarioRolesRequestDtoValidator()
    {
        RuleFor(request => request.Roles).NotEmpty().WithMessage(MessageCodes.FieldRequired);
        RuleForEach(request => request.Roles).NotEmpty().WithMessage(MessageCodes.FieldRequired);
    }
}
