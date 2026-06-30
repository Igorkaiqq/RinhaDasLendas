using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record GetDraftMontagemByIdQuery(Guid Id) : IRequest<DraftMontagemResponseDto?>;
