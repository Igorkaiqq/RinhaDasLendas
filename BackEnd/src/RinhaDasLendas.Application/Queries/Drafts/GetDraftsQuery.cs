using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Drafts;

public sealed record GetDraftsQuery(string? Search, string? Status, int Page, int PageSize) : IRequest<PaginatedResponseDto<DraftResponseDto>>;
