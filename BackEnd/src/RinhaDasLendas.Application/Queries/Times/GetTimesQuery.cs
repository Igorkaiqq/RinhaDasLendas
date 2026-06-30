using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Times;

public sealed record GetTimesQuery(string? Search, string? Status, int Page, int PageSize) : IRequest<PaginatedResponseDto<TimeResponseDto>>;
