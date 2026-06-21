using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Usuarios;

public sealed record GetUsuariosQuery(string? Search, string? Nome, string? Email, string? Role, string? Status, int Page, int PageSize) : IRequest<PaginatedResponseDto<UsuarioResumoDto>>;

public sealed record GetUsuarioByIdQuery(Guid UsuarioId) : IRequest<UsuarioResponseDto?>;

public sealed record GetAssignableRolesQuery : IRequest<RoleListResponseDto>;

public sealed record GetUsuarioAuditoriaQuery(Guid UsuarioId, int Page, int PageSize) : IRequest<PaginatedResponseDto<UsuarioAuditoriaResponseDto>>;
