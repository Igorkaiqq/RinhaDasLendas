using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Usuarios;

public sealed record UpdateUsuarioCommand(Guid UsuarioId, UpdateUsuarioRequestDto Request) : IRequest<UsuarioResponseDto?>;

public sealed record UpdateUsuarioRolesCommand(Guid UsuarioId, UpdateUsuarioRolesRequestDto Request) : IRequest<UsuarioRolesResponseDto?>;

public sealed record AtivarUsuarioCommand(Guid UsuarioId) : IRequest<UsuarioResponseDto?>;

public sealed record DesativarUsuarioCommand(Guid UsuarioId) : IRequest<UsuarioResponseDto?>;

public sealed record ResetUsuarioPasswordCommand(Guid UsuarioId, ResetUsuarioPasswordRequestDto Request) : IRequest;
