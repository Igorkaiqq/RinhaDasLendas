using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateDraftValidator : AbstractValidator<CreateDraftRequestDto>
{
    public CreateDraftValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftNameRequired)
            .MaximumLength(120)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.Observacoes)
            .MaximumLength(500)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.TamanhoTime)
            .InclusiveBetween(DraftSessao.MinimoTamanhoTime, DraftSessao.MaximoTamanhoTime)
            .WithMessage(MessageCodes.TeamSizeRange);

        RuleFor(request => request.JogadoresIds)
            .NotNull()
            .WithMessage(MessageCodes.DraftPlayersRequired)
            .Must(ids => ids.Count >= 2)
            .WithMessage(MessageCodes.DraftPlayersRequired)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage(MessageCodes.DraftPlayerAlreadyPicked);

        RuleFor(request => request)
            .Must(request => request.SortearCapitaes || (request.CapitaoTimeAId.HasValue && request.CapitaoTimeBId.HasValue))
            .WithMessage(MessageCodes.DraftCaptainRequired)
            .Must(request => request.SortearCapitaes || request.CapitaoTimeAId != request.CapitaoTimeBId)
            .WithMessage(MessageCodes.DraftCaptainDistinct)
            .Must(request => request.SortearPrimeiroPick || request.PrimeiroTime is "TimeA" or "TimeB")
            .WithMessage(MessageCodes.DraftFirstPickRequired);
    }
}
