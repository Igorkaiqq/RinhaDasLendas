using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Usuarios;

public sealed record UpdateUsuarioRolesCommand(Guid UsuarioId, UpdateUsuarioRolesRequestDto Request) : IRequest<UsuarioRolesResponseDto?>;
