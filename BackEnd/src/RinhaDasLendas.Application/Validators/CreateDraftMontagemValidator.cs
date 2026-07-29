using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateDraftMontagemValidator : AbstractValidator<CreateDraftMontagemRequestDto>
{
    public CreateDraftMontagemValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty().WithMessage(MessageCodes.DraftNameRequired)
            .MaximumLength(120).WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.Observacoes)
            .MaximumLength(500).WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => request.TamanhoEquipe)
            .InclusiveBetween(DraftMontagem.MinimoTamanhoEquipe, DraftMontagem.MaximoTamanhoEquipe)
            .WithMessage(MessageCodes.TeamSizeRange);

        RuleFor(request => request.JogadoresIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(MessageCodes.DraftMontagemPlayersRequired)
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage(MessageCodes.DraftPlayerAlreadyPicked);

        RuleFor(request => request)
            .Must(request => request.JogadoresIds is not null && (request.JogadoresIds.Count == 0 || (request.TamanhoEquipe >= DraftMontagem.MinimoTamanhoEquipe && request.JogadoresIds.Count / request.TamanhoEquipe >= 1)))
            .WithMessage(MessageCodes.DraftMontagemInsufficientPlayers);

        RuleFor(request => request.DiscordGuildId)
            .MaximumLength(40).WithMessage(MessageCodes.MaxLengthExceeded);
    }
}
