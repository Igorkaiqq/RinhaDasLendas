using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

public sealed class RestaurarDraftMontagemValidator : AbstractValidator<RestaurarDraftMontagemRequestDto>
{
    public RestaurarDraftMontagemValidator()
    {
        RuleFor(request => request.VersaoEstado)
            .GreaterThanOrEqualTo(0)
            .WithMessage(MessageCodes.DraftStateVersionInvalid);
    }
}
