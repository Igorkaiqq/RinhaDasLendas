using MediatR;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record ProcessarAgendamentosPresencaDevidosCommand(
    DateTimeOffset Agora,
    Guid? Cursor = null,
    Guid? BlockedCursor = null)
    : IRequest<AgendamentoPresencaCycleResult>;

public sealed record AgendamentoPresencaCycleResult(
    int Avaliadas,
    int Criadas,
    int Bloqueadas,
    int Perdidas,
    int Falhas,
    Guid? Cursor = null,
    Guid? BlockedCursor = null);
