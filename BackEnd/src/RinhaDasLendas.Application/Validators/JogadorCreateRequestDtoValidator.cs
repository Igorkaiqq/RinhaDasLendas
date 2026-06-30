using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class JogadorCreateRequestDtoValidator : AbstractValidator<JogadorCreateRequestDto>
{
    public JogadorCreateRequestDtoValidator()
    {
        Include(new JogadorDadosBasicosValidator());

        RuleFor(request => request.Preferencias)
            .SetValidator(new PreferenciasRotasValidator());
    }
}
