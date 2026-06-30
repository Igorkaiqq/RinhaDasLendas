using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemParticipanteResponseDto(
    Guid JogadorId,
    string NomeExibicao,
    string? Discord,
    string? RiotId,
    string? OpGgUrl,
    string? DeepLolUrl,
    string? Elo,
    string? Divisao,
    string Status,
    IReadOnlyCollection<PreferenciaRotaDto> Preferencias,
    string Estado,
    bool Capitao,
    string? RotaContextual,
    int Ordem,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftMontagemParticipanteResponseDto FromEntity(DraftMontagemParticipante participante)
    {
        var jogador = participante.Jogador;
        return new DraftMontagemParticipanteResponseDto(
            participante.JogadorId,
            jogador?.NomeExibicao ?? string.Empty,
            jogador?.Discord,
            jogador?.RiotId,
            jogador?.OpGgUrl,
            jogador?.DeepLolUrl,
            jogador?.Elo.ToDisplayName(),
            jogador?.Divisao?.ToDisplayName(),
            jogador?.Status.ToString() ?? string.Empty,
            jogador?.Preferencias.OrderBy(preferencia => preferencia.Prioridade).Select(preferencia => new PreferenciaRotaDto(preferencia.Rota.ToString(), preferencia.Prioridade, preferencia.NaoJogoNemLascando)).ToList() ?? [],
            participante.Estado.ToString(),
            participante.Capitao,
            participante.RotaContextual?.ToString(),
            participante.Ordem,
            jogador?.DataCadastro ?? participante.DataCadastro,
            jogador?.DataAtualizacao ?? participante.DataAtualizacao);
    }
}
