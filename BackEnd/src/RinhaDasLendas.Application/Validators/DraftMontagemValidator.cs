using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

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
            .Must(ids => ids.Count > 0).WithMessage(MessageCodes.DraftMontagemPlayersRequired)
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage(MessageCodes.DraftPlayerAlreadyPicked);

        RuleFor(request => request)
            .Must(request => request.JogadoresIds is not null && request.TamanhoEquipe >= DraftMontagem.MinimoTamanhoEquipe && request.JogadoresIds.Count / request.TamanhoEquipe >= 1)
            .WithMessage(MessageCodes.DraftMontagemInsufficientPlayers)
            .Must(request => request.SortearCapitaes || (request.JogadoresIds is not null && request.CapitaesIds is not null && request.TamanhoEquipe >= DraftMontagem.MinimoTamanhoEquipe && request.CapitaesIds.Count == request.JogadoresIds.Count / request.TamanhoEquipe))
            .WithMessage(MessageCodes.DraftMontagemCaptainsRequired)
            .Must(request => request.SortearCapitaes || (request.CapitaesIds is not null && request.CapitaesIds.Distinct().Count() == request.CapitaesIds.Count))
            .WithMessage(MessageCodes.DraftMontagemCaptainsDistinct)
            .Must(request => request.SortearCapitaes || (request.JogadoresIds is not null && request.CapitaesIds is not null && request.CapitaesIds.All(id => request.JogadoresIds.Contains(id))))
            .WithMessage(MessageCodes.DraftMontagemCaptainsMustBePlayers);
    }
}

public sealed class SalvarLayoutDraftMontagemValidator : AbstractValidator<SalvarLayoutDraftMontagemRequestDto>
{
    public SalvarLayoutDraftMontagemValidator()
    {
        RuleFor(request => request.Times).NotNull().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Livres).NotNull().WithMessage(MessageCodes.FieldRequired);
        RuleFor(request => request.Reservas).NotNull().WithMessage(MessageCodes.FieldRequired);
        RuleForEach(request => request.Times).ChildRules(time =>
        {
            time.RuleFor(item => item.TimeId).NotEmpty().WithMessage(MessageCodes.FieldRequired);
            time.RuleFor(item => item.Nome).NotEmpty().WithMessage(MessageCodes.TeamNameRequired);
            time.RuleFor(item => item.Jogadores).NotNull().WithMessage(MessageCodes.TeamPlayersRequired);
        });
        RuleFor(request => request).Must(AllRoutesValid).WithMessage(MessageCodes.InvalidRoute);
    }

    private static bool AllRoutesValid(SalvarLayoutDraftMontagemRequestDto request)
    {
        var participantes = request.Livres.Concat(request.Reservas).Concat(request.Times.SelectMany(time => time.Jogadores));
        return participantes.All(participante => string.IsNullOrWhiteSpace(participante.RotaContextual) || Enum.TryParse<Rota>(participante.RotaContextual, true, out _));
    }
}

public sealed class CancelarDraftMontagemValidator : AbstractValidator<CancelarDraftMontagemRequestDto>
{
    public CancelarDraftMontagemValidator()
    {
        RuleFor(request => request.Motivo)
            .MaximumLength(500)
            .WithMessage(MessageCodes.CancellationReasonMaxLength);
    }
}

public sealed class RegistrarPickDraftMontagemValidator : AbstractValidator<RegistrarPickDraftMontagemRequestDto>
{
    public RegistrarPickDraftMontagemValidator()
    {
        RuleFor(request => request.JogadorId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftInvalidPlayer);
    }
}

public sealed class SubstituirReservaDraftMontagemValidator : AbstractValidator<SubstituirReservaDraftMontagemRequestDto>
{
    public SubstituirReservaDraftMontagemValidator()
    {
        RuleFor(request => request.TimeId)
            .NotEmpty()
            .WithMessage(MessageCodes.TeamNotFound);

        RuleFor(request => request.JogadorSaiuId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftMontagemPlayerNotInTeam);

        RuleFor(request => request.ReservaEntrouId)
            .NotEmpty()
            .WithMessage(MessageCodes.DraftMontagemReserveRequired);

        RuleFor(request => request.Motivo)
            .MaximumLength(500)
            .WithMessage(MessageCodes.DraftMontagemSubstitutionReasonMaxLength);
    }
}
