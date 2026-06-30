using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record AvancarTurnoDraftMontagemTimeoutCommand(Guid Id) : IRequest<DraftMontagemRealtimeStateDto?>;
