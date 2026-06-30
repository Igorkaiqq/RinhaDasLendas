using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record SubstituirReservaDraftMontagemCommand(Guid Id, SubstituirReservaDraftMontagemRequestDto Request) : IRequest<DraftMontagemRealtimeStateDto?>;
