using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Jogadores;

internal static class JogadorTestData
{
    public static IReadOnlyCollection<PreferenciaRotaDto> PreferenciasValidas()
    {
        return
        [
            new("Top", 1, false),
            new("Jungle", 2, false),
            new("Mid", 3, false),
            new("Adc", 4, false),
            new("Support", 5, false)
        ];
    }

    public static JogadorCreateRequestDto CreateRequest(string nome = "Faker da Firma")
    {
        return new JogadorCreateRequestDto(
            nome,
            "Joao Silva",
            "joao#1234",
            "Faker#BR1",
            "https://www.op.gg/summoners/br/Faker-BR1",
            "https://www.deeplol.gg/summoner/br/Faker-BR1",
            "Ouro",
            "II",
            PreferenciasValidas());
    }

    public static Jogador JogadorAtivo(string nome = "Faker da Firma")
    {
        return new Jogador(
            nome,
            null,
            "joao#1234",
            null,
            null,
            null,
            Elo.Ouro,
            Divisao.II,
            PreferenciasValidas().Select(preferencia => new PreferenciaRota(
                Enum.Parse<Rota>(preferencia.Rota),
                preferencia.Prioridade,
                preferencia.NaoJogoNemLascando)));
    }
}
