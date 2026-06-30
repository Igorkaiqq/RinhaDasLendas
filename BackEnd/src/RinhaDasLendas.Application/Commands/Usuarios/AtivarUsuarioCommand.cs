using MediatR;
using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Commands.Usuarios;

public sealed record AtivarUsuarioCommand(Guid UsuarioId) : IRequest<UsuarioResponseDto?>;
