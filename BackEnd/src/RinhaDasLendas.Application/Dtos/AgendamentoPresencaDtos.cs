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
    OcorrenciaAgendamentoPresencaSummaryDto? UltimaOcorrencia);

public sealed record OcorrenciaAgendamentoPresencaSummaryDto(
    Guid Id,
    DateOnly DataLocal,
    DateTimeOffset PublicacaoPrevistaEm,
    DateTimeOffset EncerramentoPrevistoEm,
    OcorrenciaAgendamentoPresencaStatus Status,
    Guid? DraftMontagemId,
    string? MessageCode);
