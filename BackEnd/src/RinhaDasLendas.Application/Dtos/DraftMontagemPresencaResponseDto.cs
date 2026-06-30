using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemPresencaResponseDto(
    Guid Id,
    Guid UsuarioId,
    Guid JogadorId,
    string NomeExibicao,
    string? DiscordUserId,
    string OrigemConfirmacao,
    string Status,
    DateTimeOffset ConfirmadoEm,
    DateTimeOffset? CanceladoEm,
    int OrdemConfirmacao,
    int? OrdemManual,
    int? OrdemFinal)
{
    public static DraftMontagemPresencaResponseDto FromEntity(DraftMontagemPresenca presenca)
    {
        return new DraftMontagemPresencaResponseDto(
            presenca.Id,
            presenca.UsuarioId,
            presenca.JogadorId,
            presenca.Jogador?.NomeExibicao ?? string.Empty,
            presenca.DiscordUserId,
            presenca.OrigemConfirmacao.ToString(),
            presenca.Status.ToString(),
            presenca.ConfirmadoEm,
            presenca.CanceladoEm,
            presenca.OrdemConfirmacao,
            presenca.OrdemManual,
            presenca.OrdemFinal);
    }
}
