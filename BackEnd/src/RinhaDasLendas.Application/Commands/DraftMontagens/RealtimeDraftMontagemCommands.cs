using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record IniciarDraftMontagemTempoRealCommand(Guid Id) : IRequest<DraftMontagemRealtimeStateDto?>;

public sealed record RegistrarPickDraftMontagemCommand(Guid Id, RegistrarPickDraftMontagemRequestDto Request) : IRequest<DraftMontagemRealtimeStateDto?>;

public sealed record AvancarTurnoDraftMontagemTimeoutCommand(Guid Id) : IRequest<DraftMontagemRealtimeStateDto?>;

public sealed record SubstituirReservaDraftMontagemCommand(Guid Id, SubstituirReservaDraftMontagemRequestDto Request) : IRequest<DraftMontagemRealtimeStateDto?>;
