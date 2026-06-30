using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record GetDraftMontagensQuery(string? Search, string? Status, int Page, int PageSize) : IRequest<PaginatedResponseDto<DraftMontagemResumoDto>>;
