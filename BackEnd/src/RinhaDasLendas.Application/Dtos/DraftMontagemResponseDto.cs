using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DraftMontagemResponseDto(
    Guid Id,
    string Nome,
    string? Observacoes,
    string Status,
    string Modo,
    int TamanhoEquipe,
    int QuantidadeTimes,
    int QuantidadeReservas,
    string CriterioCapitaes,
    Guid? TurnoAtualTimeId,
    Guid? TurnoAtualCapitaoId,
    int? TurnoSequencia,
    DateTimeOffset? TurnoIniciadoEm,
    DateTimeOffset? TurnoExpiraEm,
    int DuracaoTurnoSegundos,
    DateTimeOffset? HorarioEncerramentoPresenca,
    string? DiscordGuildId,
    string? DiscordPresenceMessageId,
    string? OrdemEscolhaModo,
    bool PresencaContinuadaManualmente,
    IReadOnlyCollection<DraftMontagemPresencaResponseDto> Presencas,
    IReadOnlyCollection<DraftMontagemTimeResponseDto> Times,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Livres,
    IReadOnlyCollection<DraftMontagemParticipanteResponseDto> Reservas,
    IReadOnlyCollection<DraftMontagemEscolhaResponseDto> Escolhas,
    IReadOnlyCollection<DraftMontagemSubstituicaoResponseDto> Substituicoes,
    string? MotivoCancelamento,
    DateTimeOffset DataCadastro,
    DateTimeOffset DataAtualizacao)
{
    public static DraftMontagemResponseDto FromEntity(DraftMontagem montagem)
    {
        var participantes = montagem.Participantes.ToList();
        return new DraftMontagemResponseDto(
            montagem.Id,
            montagem.Nome,
            montagem.Observacoes,
            montagem.Status.ToString(),
            montagem.Modo.ToString(),
            montagem.TamanhoEquipe,
            montagem.QuantidadeTimes,
            montagem.QuantidadeReservas,
            montagem.CriterioCapitaes.ToString(),
            montagem.TurnoAtualTimeId,
            montagem.TurnoAtualCapitaoId,
            montagem.TurnoSequencia,
            montagem.TurnoIniciadoEm,
            montagem.TurnoExpiraEm,
            montagem.DuracaoTurnoSegundos,
            montagem.HorarioEncerramentoPresenca,
            montagem.DiscordGuildId,
            montagem.DiscordPresenceMessageId,
            montagem.OrdemEscolhaModo?.ToString(),
            montagem.PresencaContinuadaManualmente,
            montagem.Presencas.OrderBy(presenca => presenca.OrdemFinal ?? presenca.OrdemManual ?? presenca.OrdemConfirmacao).Select(DraftMontagemPresencaResponseDto.FromEntity).ToList(),
            montagem.Times.OrderBy(time => time.Ordem).Select(time => DraftMontagemTimeResponseDto.FromEntity(time, participantes)).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Livre).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            participantes.Where(participante => participante.Estado == DraftMontagemParticipanteEstado.Reserva).OrderBy(participante => participante.Ordem).Select(DraftMontagemParticipanteResponseDto.FromEntity).ToList(),
            montagem.Escolhas.OrderBy(escolha => escolha.Sequencia).Select(DraftMontagemEscolhaResponseDto.FromEntity).ToList(),
            montagem.Substituicoes.OrderBy(substituicao => substituicao.RegistradoEm).Select(DraftMontagemSubstituicaoResponseDto.FromEntity).ToList(),
            montagem.MotivoCancelamento,
            montagem.DataCadastro,
            montagem.DataAtualizacao);
    }
}
