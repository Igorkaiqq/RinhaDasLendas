using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.DraftMontagens;

public sealed record GetJogadoresElegiveisPresencaDraftMontagemQuery(Guid DraftMontagemId, string? Search, int Page, int PageSize) : IRequest<PaginatedResponseDto<DraftMontagemJogadorElegivelPresencaDto>>;
