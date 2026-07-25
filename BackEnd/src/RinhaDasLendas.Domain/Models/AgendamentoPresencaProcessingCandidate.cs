using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Domain.Models;

public sealed record AgendamentoPresencaProcessingCandidate(
    AgendamentoPresenca Agenda,
    uint Version);
