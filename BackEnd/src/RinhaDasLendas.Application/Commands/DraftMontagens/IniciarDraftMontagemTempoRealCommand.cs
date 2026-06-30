using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.DraftMontagens;

public sealed record IniciarDraftMontagemTempoRealCommand(Guid Id) : IRequest<DraftMontagemRealtimeStateDto?>;
