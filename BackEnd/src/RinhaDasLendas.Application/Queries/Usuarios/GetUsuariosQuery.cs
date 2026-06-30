using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Usuarios;

public sealed record GetUsuariosQuery(string? Search, string? Nome, string? Email, string? Role, string? Status, int Page, int PageSize) : IRequest<PaginatedResponseDto<UsuarioResumoDto>>;
