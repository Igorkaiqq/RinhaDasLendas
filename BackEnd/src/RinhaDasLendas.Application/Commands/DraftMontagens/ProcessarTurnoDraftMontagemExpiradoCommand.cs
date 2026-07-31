using MediatR;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record ProcessarTurnoDraftMontagemExpiradoCommand(
    Guid Id,
    TimeSpan MaxRealtimeDuration) : IRequest;
