using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateDraftValidator : AbstractValidator<CreateDraftRequestDto>
{
    public CreateDraftValidator()
    {
        RuleFor(request => request.Nome)
            .NotEmpty()
            .WithMessage("Nome do draft e obrigatorio.")
            .MaximumLength(120)
            .WithMessage("Nome do draft deve ter no maximo 120 caracteres.");

        RuleFor(request => request.Observacoes)
            .MaximumLength(500)
            .WithMessage("Observacoes devem ter no maximo 500 caracteres.");

        RuleFor(request => request.TamanhoTime)
            .InclusiveBetween(DraftSessao.MinimoTamanhoTime, DraftSessao.MaximoTamanhoTime)
            .WithMessage("Tamanho do time deve estar entre 1 e 5.");

        RuleFor(request => request.JogadoresIds)
            .NotNull()
            .WithMessage("Informe os jogadores do draft.")
            .Must(ids => ids.Count >= 2)
            .WithMessage("Informe pelo menos dois jogadores para o draft.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("O mesmo jogador nao pode aparecer mais de uma vez no draft.");

        RuleFor(request => request)
            .Must(request => request.SortearCapitaes || (request.CapitaoTimeAId.HasValue && request.CapitaoTimeBId.HasValue))
            .WithMessage("Informe os capitaes ou habilite o sorteio.")
            .Must(request => request.SortearCapitaes || request.CapitaoTimeAId != request.CapitaoTimeBId)
            .WithMessage("Informe dois capitaes distintos para o draft.")
            .Must(request => request.SortearPrimeiroPick || request.PrimeiroTime is "TimeA" or "TimeB")
            .WithMessage("Informe o primeiro time ou habilite o sorteio.");
    }
}

public sealed class RegistrarPickDraftValidator : AbstractValidator<RegistrarPickDraftRequestDto>
{
    public RegistrarPickDraftValidator()
    {
        RuleFor(request => request.JogadorId)
            .NotEmpty()
            .WithMessage("Jogador da escolha e obrigatorio.");
    }
}

public sealed class CancelarDraftValidator : AbstractValidator<CancelarDraftRequestDto>
{
    public CancelarDraftValidator()
    {
        RuleFor(request => request.Motivo)
            .MaximumLength(500)
            .WithMessage("Motivo do cancelamento deve ter no maximo 500 caracteres.");
    }
}
