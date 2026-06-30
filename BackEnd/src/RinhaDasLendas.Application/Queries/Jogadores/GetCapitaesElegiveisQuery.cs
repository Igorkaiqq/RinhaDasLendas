using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Jogadores;

public sealed record GetCapitaesElegiveisQuery(int Page, int PageSize) : IRequest<PaginatedResponseDto<JogadorResponseDto>>;
