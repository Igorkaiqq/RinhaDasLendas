using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

internal static class JogadorEloMapper
{
    public static (Elo Elo, Divisao? Divisao) ToEnums(IJogadorDadosBasicos request)
    {
        EloExtensions.TryParseDisplayName(request.Elo, out var elo);

        Divisao? divisao = null;
        if (DivisaoExtensions.TryParseDisplayName(request.Divisao, out var parsedDivisao))
        {
            divisao = parsedDivisao;
        }

        return (elo, divisao);
    }
}
