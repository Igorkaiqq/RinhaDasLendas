using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class UpdatePreferenciasRotasRequestDtoValidator : AbstractValidator<UpdatePreferenciasRotasRequestDto>
{
    public UpdatePreferenciasRotasRequestDtoValidator()
    {
        RuleFor(request => request.Preferencias)
            .SetValidator(new PreferenciasRotasValidator());
    }
}
