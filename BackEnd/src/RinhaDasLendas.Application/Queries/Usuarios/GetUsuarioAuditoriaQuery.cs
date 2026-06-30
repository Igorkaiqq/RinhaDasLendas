using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Usuarios;

public sealed record GetUsuarioAuditoriaQuery(Guid UsuarioId, int Page, int PageSize) : IRequest<PaginatedResponseDto<UsuarioAuditoriaResponseDto>>;
