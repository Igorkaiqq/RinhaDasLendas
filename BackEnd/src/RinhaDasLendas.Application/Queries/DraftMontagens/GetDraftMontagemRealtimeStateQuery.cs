using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record GetDraftMontagemRealtimeStateQuery(Guid Id) : IRequest<DraftMontagemRealtimeStateDto?>;
