using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record JogadorResponseDto(
    Guid Id,
    string NomeExibicao,
    string? NomeReal,
    string? Discord,
    string? RiotId,
    string? OpGgUrl,
    string? DeepLolUrl,
    string? Elo,
    string? Divisao,
    string Status,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao,
    IReadOnlyCollection<PreferenciaRotaDto> Preferencias)
{
    public static JogadorResponseDto FromEntity(Jogador jogador)
    {
        return new JogadorResponseDto(
            jogador.Id,
            jogador.NomeExibicao,
            jogador.NomeReal,
            jogador.Discord,
            jogador.RiotId,
            jogador.OpGgUrl,
            jogador.DeepLolUrl,
            jogador.Elo.ToDisplayName(),
            jogador.Divisao?.ToDisplayName(),
            jogador.Status.ToString(),
            jogador.DataCadastro,
            jogador.DataAtualizacao,
            jogador.Preferencias
                .OrderBy(preferencia => preferencia.Prioridade)
                .Select(preferencia => new PreferenciaRotaDto(
                    preferencia.Rota.ToString(),
                    preferencia.Prioridade,
                    preferencia.NaoJogoNemLascando))
                .ToList());
    }
}
