using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Queries.Usuarios;

public sealed record GetAssignableRolesQuery : IRequest<RoleListResponseDto>;
