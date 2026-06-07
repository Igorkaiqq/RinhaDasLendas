using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Handlers.Jogadores;

internal static class JogadorHandlerHelpers
{
    public static IReadOnlyCollection<PreferenciaRota> ToPreferencias(IReadOnlyCollection<PreferenciaRotaDto> preferencias)
    {
        return preferencias
            .Select(preferencia => new PreferenciaRota(
                Enum.Parse<Rota>(preferencia.Rota, ignoreCase: true),
                preferencia.Prioridade,
                preferencia.NaoJogoNemLascando))
            .ToList();
    }
}
