using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Jogadores;

public sealed record GetJogadoresQuery(bool SomenteAtivos, int Page, int PageSize) : IRequest<PaginatedResponseDto<JogadorResponseDto>>;
