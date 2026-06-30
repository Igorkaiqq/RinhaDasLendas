using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Drafts;

public sealed record GetDraftByIdQuery(Guid DraftId) : IRequest<DraftResponseDto?>;
