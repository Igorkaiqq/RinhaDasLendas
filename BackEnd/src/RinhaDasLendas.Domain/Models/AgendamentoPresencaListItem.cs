using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Domain.Models;

public sealed record AgendamentoPresencaListItem(
    AgendamentoPresenca Agenda,
    DateTimeOffset? ProximaExecucaoEm);
