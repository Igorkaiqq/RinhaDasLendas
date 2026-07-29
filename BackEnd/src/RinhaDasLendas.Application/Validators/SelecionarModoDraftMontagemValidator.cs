using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class SelecionarModoDraftMontagemValidator : AbstractValidator<SelecionarModoDraftMontagemRequestDto>
{
    public SelecionarModoDraftMontagemValidator()
    {
        RuleFor(request => request.Modo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(MessageCodes.FieldRequired)
            .Must(ModoValido).WithMessage(MessageCodes.FieldRequired);
    }

    private static bool ModoValido(string modo)
    {
        return !int.TryParse(modo, out _)
            && Enum.TryParse<DraftMontagemModo>(modo, true, out var parsed)
            && Enum.IsDefined(parsed);
    }
}
