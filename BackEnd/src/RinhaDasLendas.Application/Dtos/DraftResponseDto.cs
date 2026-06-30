using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftResponseDto(
    Guid Id,
    string Nome,
    string? Observacoes,
    string Status,
    int TamanhoTime,
    string CriterioCapitaes,
    string CriterioPrimeiroPick,
    string? ProximoTime,
    DraftJogadorDto CapitaoTimeA,
    DraftJogadorDto CapitaoTimeB,
    IReadOnlyCollection<DraftParticipanteDto> TimeA,
    IReadOnlyCollection<DraftParticipanteDto> TimeB,
    IReadOnlyCollection<DraftJogadorDto> Disponiveis,
    IReadOnlyCollection<DraftEscolhaDto> Escolhas,
    string? MotivoCancelamento,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftResponseDto FromEntity(DraftSessao draft)
    {
        var participantes = draft.Participantes.ToList();
        var jogadores = participantes.ToDictionary(item => item.JogadorId, item => item.Jogador?.NomeExibicao ?? string.Empty);
        var capitaoA = new DraftJogadorDto(draft.CapitaoTimeAId, jogadores.GetValueOrDefault(draft.CapitaoTimeAId, string.Empty));
        var capitaoB = new DraftJogadorDto(draft.CapitaoTimeBId, jogadores.GetValueOrDefault(draft.CapitaoTimeBId, string.Empty));

        return new DraftResponseDto(
            draft.Id,
            draft.Nome,
            draft.Observacoes,
            draft.Status.ToString(),
            draft.TamanhoTime,
            draft.CriterioCapitaes.ToString(),
            draft.CriterioPrimeiroPick.ToString(),
            draft.ProximoTime?.ToString(),
            capitaoA,
            capitaoB,
            MapParticipantes(participantes, DraftTime.TimeA),
            MapParticipantes(participantes, DraftTime.TimeB),
            participantes.Where(item => item.Disponivel).OrderBy(item => item.Jogador?.NomeExibicao).Select(item => new DraftJogadorDto(item.JogadorId, item.Jogador?.NomeExibicao ?? string.Empty)).ToList(),
            draft.Escolhas.OrderBy(escolha => escolha.Sequencia).Select(escolha => new DraftEscolhaDto(escolha.Sequencia, escolha.Time.ToString(), escolha.CapitaoId, escolha.JogadorId, escolha.Jogador?.NomeExibicao ?? jogadores.GetValueOrDefault(escolha.JogadorId, string.Empty), escolha.DataEscolha)).ToList(),
            draft.MotivoCancelamento,
            draft.DataCadastro,
            draft.DataAtualizacao);
    }

    private static IReadOnlyCollection<DraftParticipanteDto> MapParticipantes(IReadOnlyCollection<DraftParticipante> participantes, DraftTime time)
    {
        return participantes
            .Where(participante => participante.Time == time)
            .OrderByDescending(participante => participante.Capitao)
            .ThenBy(participante => participante.Jogador?.NomeExibicao)
            .Select(participante => new DraftParticipanteDto(participante.JogadorId, participante.Jogador?.NomeExibicao ?? string.Empty, participante.Capitao))
            .ToList();
    }
}
