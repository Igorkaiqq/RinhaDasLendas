using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record SaveAgendamentoPresencaRequestDto(
    string Nome,
    string? Observacao,
    IReadOnlyCollection<DiaSemanaIso> DiasSemana,
    TimeOnly HorarioPublicacao,
    TimeOnly HorarioEncerramento);

public sealed record AgendamentoPresencaSummaryDto(
    Guid Id,
    string Nome,
    string? Observacao,
    AgendamentoPresencaStatus Status,
    IReadOnlyCollection<DiaSemanaIso> DiasSemana,
    TimeOnly HorarioPublicacao,
    TimeOnly HorarioEncerramento,
    DateTimeOffset? ProximaExecucaoEm,
    OcorrenciaAgendamentoPresencaSummaryDto? UltimaOcorrencia)
{
    public static AgendamentoPresencaSummaryDto FromEntity(
        AgendamentoPresenca agenda,
        IAgendamentoPresencaTimeZone timeZone,
        OcorrenciaAgendamentoPresenca? latestOccurrence = null)
    {
        var ultimaOcorrencia = latestOccurrence ?? agenda.Ocorrencias
            .OrderByDescending(item => item.DataLocal)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        return new AgendamentoPresencaSummaryDto(
            agenda.Id,
            agenda.Nome,
            agenda.Observacao,
            agenda.Status,
            agenda.DiasSemana.Select(item => item.DiaSemana).Order().ToArray(),
            agenda.HorarioPublicacaoLocal,
            agenda.HorarioEncerramentoLocal,
            CalculateNextExecution(agenda, timeZone),
            ultimaOcorrencia is null ? null : OcorrenciaAgendamentoPresencaSummaryDto.FromEntity(ultimaOcorrencia));
    }

    private static DateTimeOffset? CalculateNextExecution(
        AgendamentoPresenca agenda,
        IAgendamentoPresencaTimeZone timeZone)
    {
        if (agenda.Status != AgendamentoPresencaStatus.Ativo)
        {
            return null;
        }

        for (var offset = 1; offset <= 7; offset++)
        {
            var date = agenda.UltimaDataAvaliada.AddDays(offset);
            if (agenda.OcorreEm(date))
            {
                return timeZone.ToUtc(date, agenda.HorarioPublicacaoLocal);
            }
        }

        return null;
    }
}

public sealed record OcorrenciaAgendamentoPresencaSummaryDto(
    Guid Id,
    DateOnly DataLocal,
    DateTimeOffset PublicacaoPrevistaEm,
    DateTimeOffset EncerramentoPrevistoEm,
    OcorrenciaAgendamentoPresencaStatus Status,
    Guid? DraftMontagemId,
    string? MessageCode)
{
    public static OcorrenciaAgendamentoPresencaSummaryDto FromEntity(OcorrenciaAgendamentoPresenca occurrence) => new(
        occurrence.Id,
        occurrence.DataLocal,
        occurrence.PublicacaoPrevistaEm,
        occurrence.EncerramentoPrevistoEm,
        occurrence.Status,
        occurrence.DraftMontagemId,
        occurrence.CodigoFalha);
}
