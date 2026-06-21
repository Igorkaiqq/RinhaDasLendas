using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateDraftMontagemValidator : AbstractValidator<CreateDraftMontagemRequestDto>
{
    public CreateDraftMontagemValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty().WithMessage("Nome da montagem e obrigatorio.")
            .MaximumLength(120).WithMessage("Nome da montagem deve ter no maximo 120 caracteres.");

        RuleFor(request => request.Observacoes)
            .MaximumLength(500).WithMessage("Observacoes devem ter no maximo 500 caracteres.");

        RuleFor(request => request.TamanhoEquipe)
            .InclusiveBetween(DraftMontagem.MinimoTamanhoEquipe, DraftMontagem.MaximoTamanhoEquipe)
            .WithMessage("Tamanho da equipe deve estar entre 1 e 5.");

        RuleFor(request => request.JogadoresIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Informe os jogadores da montagem.")
            .Must(ids => ids.Count > 0).WithMessage("Informe ao menos um jogador.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("O mesmo jogador nao pode aparecer mais de uma vez na montagem.");

        RuleFor(request => request)
            .Must(request => request.JogadoresIds is not null && request.TamanhoEquipe >= DraftMontagem.MinimoTamanhoEquipe && request.JogadoresIds.Count / request.TamanhoEquipe >= 1)
            .WithMessage("Jogadores insuficientes para formar um time completo.")
            .Must(request => request.SortearCapitaes || (request.JogadoresIds is not null && request.CapitaesIds is not null && request.TamanhoEquipe >= DraftMontagem.MinimoTamanhoEquipe && request.CapitaesIds.Count == request.JogadoresIds.Count / request.TamanhoEquipe))
            .WithMessage("Informe um capitao para cada time gerado ou habilite o sorteio.")
            .Must(request => request.SortearCapitaes || (request.CapitaesIds is not null && request.CapitaesIds.Distinct().Count() == request.CapitaesIds.Count))
            .WithMessage("Capitaes devem ser distintos.")
            .Must(request => request.SortearCapitaes || (request.JogadoresIds is not null && request.CapitaesIds is not null && request.CapitaesIds.All(id => request.JogadoresIds.Contains(id))))
            .WithMessage("Capitaes devem fazer parte dos jogadores selecionados.");
    }
}

public sealed class SalvarLayoutDraftMontagemValidator : AbstractValidator<SalvarLayoutDraftMontagemRequestDto>
{
    public SalvarLayoutDraftMontagemValidator()
    {
        RuleFor(request => request.Times).NotNull().WithMessage("Informe os times da montagem.");
        RuleFor(request => request.Livres).NotNull().WithMessage("Informe os jogadores livres.");
        RuleFor(request => request.Reservas).NotNull().WithMessage("Informe os reservas.");
        RuleForEach(request => request.Times).ChildRules(time =>
        {
            time.RuleFor(item => item.TimeId).NotEmpty().WithMessage("Time da montagem e obrigatorio.");
            time.RuleFor(item => item.Nome).NotEmpty().WithMessage("Nome do time e obrigatorio.");
            time.RuleFor(item => item.Jogadores).NotNull().WithMessage("Informe jogadores do time.");
        });
        RuleFor(request => request).Must(AllRoutesValid).WithMessage("Informe apenas rotas validas.");
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
            .WithMessage("Motivo do cancelamento deve ter no maximo 500 caracteres.");
    }
}
