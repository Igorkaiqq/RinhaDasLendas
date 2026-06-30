using FluentValidation;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Application.Validators;

internal sealed class TimeRequestValidator<T> : AbstractValidator<T> where T : class
{
    public TimeRequestValidator()
    {
        RuleFor(request => GetNome(request))
            .NotEmpty()
            .WithMessage(MessageCodes.TeamNameRequired)
            .MaximumLength(100)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => GetTag(request))
            .NotEmpty()
            .WithMessage(MessageCodes.TeamTagRequired)
            .MaximumLength(10)
            .WithMessage(MessageCodes.MaxLengthExceeded);

        RuleFor(request => GetJogadoresIds(request))
            .NotNull()
            .WithMessage(MessageCodes.TeamPlayersRequired)
            .Must(ids => ids.Count > 0)
            .WithMessage(MessageCodes.TeamPlayersRequired)
            .Must(ids => ids.Count <= Time.MaximoJogadoresPrincipais)
            .WithMessage(MessageCodes.TeamPlayerLimitReached)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage(MessageCodes.PlayerAlreadyInTeam);

        RuleFor(request => request)
            .Must(request => GetCapitaoId(request) is null || GetJogadoresIds(request).Contains(GetCapitaoId(request)!.Value))
            .WithMessage(MessageCodes.TeamCaptainMustBeMember);
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
