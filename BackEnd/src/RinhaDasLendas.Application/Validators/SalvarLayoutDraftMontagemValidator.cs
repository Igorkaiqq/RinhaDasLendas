using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

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
