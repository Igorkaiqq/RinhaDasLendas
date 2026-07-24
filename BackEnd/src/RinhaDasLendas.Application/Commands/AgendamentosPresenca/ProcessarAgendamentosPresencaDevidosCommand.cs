using MediatR;

namespace RinhaDasLendas.Application.Commands.AgendamentosPresenca;

public sealed record ProcessarAgendamentosPresencaDevidosCommand(DateTimeOffset Agora)
    : IRequest<AgendamentoPresencaCycleResult>;

public sealed record AgendamentoPresencaCycleResult(
    int Avaliadas,
    int Criadas,
    int Bloqueadas,
    int Perdidas,
    int Falhas);
