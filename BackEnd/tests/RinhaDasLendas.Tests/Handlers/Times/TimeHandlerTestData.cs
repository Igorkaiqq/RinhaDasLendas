using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Handlers.Times;

internal static class TimeHandlerTestData
{
    public static Jogador CreateJogador(string nome = "Jogador Teste")
    {
        return new Jogador(
            nome,
            null,
            $"{nome.Replace(" ", string.Empty).ToLowerInvariant()}#1234",
            null,
            null,
            null,
            Elo.Ouro,
            Divisao.II,
            [
                new PreferenciaRota(Rota.Top, 1, false),
                new PreferenciaRota(Rota.Jungle, 2, false),
                new PreferenciaRota(Rota.Mid, 3, false),
                new PreferenciaRota(Rota.Adc, 4, false),
                new PreferenciaRota(Rota.Support, 5, false)
            ]);
    }

    public static CreateTimeRequestDto CreateRequest(Guid jogadorId, Guid? capitaoId = null, string nome = "Os Sem Baron", string tag = "OSB")
    {
        return new CreateTimeRequestDto(nome, tag, null, capitaoId ?? jogadorId, [jogadorId]);
    }

    public static UpdateTimeRequestDto UpdateRequest(IReadOnlyCollection<Guid> jogadoresIds, Guid? capitaoId, string nome = "Os Sem Baron Atualizado", string tag = "OSB")
    {
        return new UpdateTimeRequestDto(nome, tag, "Atualizado", capitaoId, jogadoresIds);
    }
}
