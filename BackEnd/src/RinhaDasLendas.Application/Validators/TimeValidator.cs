using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Application.Validators;

public sealed class CreateTimeValidator : AbstractValidator<CreateTimeRequestDto>
{
    public CreateTimeValidator()
    {
        Include(new TimeRequestValidator<CreateTimeRequestDto>());
    }
}

public sealed class UpdateTimeValidator : AbstractValidator<UpdateTimeRequestDto>
{
    public UpdateTimeValidator()
    {
        Include(new TimeRequestValidator<UpdateTimeRequestDto>());
    }
}

internal sealed class TimeRequestValidator<T> : AbstractValidator<T> where T : class
{
    public TimeRequestValidator()
    {
        RuleFor(request => GetNome(request))
            .NotEmpty()
            .WithMessage("Nome do time e obrigatorio.")
            .MaximumLength(100)
            .WithMessage("Nome do time deve ter no maximo 100 caracteres.");

        RuleFor(request => GetTag(request))
            .NotEmpty()
            .WithMessage("Tag do time e obrigatoria.")
            .MaximumLength(10)
            .WithMessage("Tag do time deve ter no maximo 10 caracteres.");

        RuleFor(request => GetJogadoresIds(request))
            .NotNull()
            .WithMessage("Informe os jogadores do time.")
            .Must(ids => ids.Count > 0)
            .WithMessage("Informe pelo menos um jogador para o time.")
            .Must(ids => ids.Count <= Time.MaximoJogadoresPrincipais)
            .WithMessage("Um time pode ter no maximo cinco jogadores.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("O mesmo jogador nao pode ser adicionado mais de uma vez.");

        RuleFor(request => request)
            .Must(request => GetCapitaoId(request) is null || GetJogadoresIds(request).Contains(GetCapitaoId(request)!.Value))
            .WithMessage("Capitao deve fazer parte do time.");
    }

    private static string GetNome(T request)
    {
        return request switch
        {
            CreateTimeRequestDto dto => dto.Nome,
            UpdateTimeRequestDto dto => dto.Nome,
            _ => string.Empty
        };
    }

    private static string GetTag(T request)
    {
        return request switch
        {
            CreateTimeRequestDto dto => dto.Tag,
            UpdateTimeRequestDto dto => dto.Tag,
            _ => string.Empty
        };
    }

    private static Guid? GetCapitaoId(T request)
    {
        return request switch
        {
            CreateTimeRequestDto dto => dto.CapitaoId,
            UpdateTimeRequestDto dto => dto.CapitaoId,
            _ => null
        };
    }

    private static IReadOnlyCollection<Guid> GetJogadoresIds(T request)
    {
        return request switch
        {
            CreateTimeRequestDto dto => dto.JogadoresIds,
            UpdateTimeRequestDto dto => dto.JogadoresIds,
            _ => []
        };
    }
}
