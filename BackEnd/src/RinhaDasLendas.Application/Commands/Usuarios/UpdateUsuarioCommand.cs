using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Usuarios;

public sealed record UpdateUsuarioCommand(Guid UsuarioId, UpdateUsuarioRequestDto Request) : IRequest<UsuarioResponseDto?>;
